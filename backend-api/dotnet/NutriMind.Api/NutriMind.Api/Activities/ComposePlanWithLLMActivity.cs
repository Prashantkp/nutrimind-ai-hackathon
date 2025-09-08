using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using NutriMind.Api.Models;
using NutriMind.Api.Services;

namespace NutriMind.Api.Activities
{
    public class ComposePlanWithLLMActivity
    {
        private readonly ILogger<ComposePlanWithLLMActivity> _logger;
        private readonly IOpenAIService _openAIService;

        public ComposePlanWithLLMActivity(
            ILogger<ComposePlanWithLLMActivity> logger,
            IOpenAIService openAIService)
        {
            _logger = logger;
            _openAIService = openAIService;
        }

        [Function("ComposePlanWithLLM")]
        public async Task<MealPlan> Run([ActivityTrigger] ComposePlanInput input)
        {
            try
            {
                _logger.LogInformation("Composing meal plan with LLM for user: {UserId}", input.UserProfile.UserId);

                // Generate the meal plan using AI
                var aiResponse = await _openAIService.GenerateWeeklyMealPlanAsync(
                    input.UserProfile, 
                    input.CandidateRecipes
                );

                // Parse the AI response into a structured meal plan
                var mealPlan = await _openAIService.ParseMealPlanResponseAsync(
                    aiResponse, 
                    input.UserProfile.UserId, 
                    input.WeekIdentifier
                );

                // Set additional metadata
                mealPlan.GenerationMetadata.AiModel = "gpt-4";
                mealPlan.GenerationMetadata.RecipesConsidered = input.CandidateRecipes.Count;
                mealPlan.GenerationMetadata.StartedAt = DateTime.UtcNow;

                // Apply user preferences to the meal plan
                mealPlan.Preferences = new MealPlanPreferences
                {
                    DietaryPreference = input.UserProfile.DietaryPreference,
                    Allergens = input.UserProfile.Allergens,
                    Dislikes = input.UserProfile.Dislikes,
                    TargetCalories = input.UserProfile.TargetCalories,
                    MaxPrepTime = input.UserProfile.CookingTimePreference,
                    BudgetConstraint = input.UserProfile.BudgetPerWeek
                };

                _logger.LogInformation("Successfully composed meal plan for user: {UserId}, Plan ID: {MealPlanId}", 
                    input.UserProfile.UserId, mealPlan.Id);

                return mealPlan;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error composing meal plan with LLM for user: {UserId}", input.UserProfile.UserId);
                
                // Return a fallback meal plan
                return CreateFallbackMealPlan(input);
            }
        }

        private MealPlan CreateFallbackMealPlan(ComposePlanInput input)
        {
            _logger.LogWarning("Creating fallback meal plan for user: {UserId}", input.UserProfile.UserId);

            var mealPlan = new MealPlan
            {
                Id = Guid.NewGuid().ToString(),
                UserId = input.UserProfile.UserId,
                WeekIdentifier = input.WeekIdentifier,
                WeekOf = Helpers.WeekHelper.GetMondayOfWeek(input.WeekIdentifier),
                Status = MealPlanStatus.Generated,
                GenerationMetadata = new GenerationMetadata
                {
                    AiModel = "fallback",
                    RecipesConsidered = input.CandidateRecipes.Count,
                    StartedAt = DateTime.UtcNow,
                    Errors = new List<string> { "AI service unavailable, using fallback generation" }
                }
            };

            // Create simple meal plan using available recipes
            var daysOfWeek = new[] { "monday", "tuesday", "wednesday", "thursday", "friday", "saturday", "sunday" };
            var mealTypes = new[] { "breakfast", "lunch", "dinner" };

            for (int dayIndex = 0; dayIndex < daysOfWeek.Length; dayIndex++)
            {
                var dayName = daysOfWeek[dayIndex];
                var dailyPlan = new DailyMealPlan
                {
                    Date = mealPlan.WeekOf.AddDays(dayIndex),
                    DayOfWeek = dayName
                };

                // Assign random recipes to each meal
                var random = new Random();
                for (int mealIndex = 0; mealIndex < mealTypes.Length; mealIndex++)
                {
                    var mealType = mealTypes[mealIndex];
                    var recipe = input.CandidateRecipes.OrderBy(r => random.Next()).FirstOrDefault();
                    
                    if (recipe != null)
                    {
                        dailyPlan.Meals[mealType] = new Meal
                        {
                            Id = Guid.NewGuid().ToString(),
                            Name = recipe.Name,
                            Recipe = recipe,
                            MealType = mealType,
                            Servings = 1,
                            EstimatedCost = 5.00m,
                            Nutrition = Helpers.NutritionCalculator.CalculateMealNutrition(recipe)
                        };
                    }
                }

                mealPlan.DailyMeals[dayName] = dailyPlan;
            }

            return mealPlan;
        }
    }

    public class ComposePlanInput
    {
        public UserProfile UserProfile { get; set; } = new();
        public List<Recipe> CandidateRecipes { get; set; } = new();
        public string WeekIdentifier { get; set; } = string.Empty;
    }
}
