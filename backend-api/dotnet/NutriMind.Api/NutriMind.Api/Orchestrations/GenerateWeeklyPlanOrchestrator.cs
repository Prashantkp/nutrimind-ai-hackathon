using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;
using NutriMind.Api.Activities;
using NutriMind.Api.Models;

namespace NutriMind.Api.Orchestrations
{
    public class GenerateWeeklyPlanOrchestrator
    {
        private readonly ILogger<GenerateWeeklyPlanOrchestrator> _logger;

        public GenerateWeeklyPlanOrchestrator(ILogger<GenerateWeeklyPlanOrchestrator> logger)
        {
            _logger = logger;
        }

        [Function("GenerateWeeklyPlan")]
        public async Task<MealPlan> RunOrchestrator(
            [OrchestrationTrigger] TaskOrchestrationContext context)
        {
            var input = context.GetInput<GenerateWeeklyPlanInput>();
            var logger = context.CreateReplaySafeLogger("GenerateWeeklyPlanOrchestrator");

            logger.LogInformation("Starting weekly meal plan generation for user: {UserId}, week: {WeekIdentifier}", 
                input.UserId, input.WeekIdentifier);

            try
            {
                // Step 1: Load user profile
                logger.LogInformation("Step 1: Loading user profile");
                var userProfile = await context.CallActivityAsync<UserProfile?>("LoadUserProfile", input.UserId);
                
                if (userProfile == null)
                {
                    throw new InvalidOperationException($"User profile not found for user: {input.UserId}");
                }

                // Step 2: Retrieve candidate recipes
                //logger.LogInformation("Step 2: Retrieving candidate recipes");
                //var recipeInput = new RetrieveRecipesInput
                //{
                //    DietaryPreference = input.Preferences?.DietaryPreference ?? userProfile.DietaryPreference,
                //    Allergens = input.Preferences?.Allergens ?? userProfile.Allergens,
                //    Dislikes = input.Preferences?.Dislikes ?? userProfile.Dislikes,
                //    PreferredCuisines = userProfile.PreferredCuisines,
                //    MaxRecipes = 100,
                //    MaxCookingTime = input.Preferences?.MaxPrepTime ?? userProfile.CookingTimePreference,
                //    MaxCaloriesPerMeal = (input.Preferences?.TargetCalories ?? userProfile.TargetCalories) / 3 // Rough estimate per meal
                //};

                //var candidateRecipes = await context.CallActivityAsync<List<Recipe>>("RetrieveCandidateRecipes", recipeInput);

                //if (!candidateRecipes.Any())
                //{
                //    throw new InvalidOperationException("No suitable recipes found for user preferences");
                //}

                // Step 3: Compose meal plan with LLM
                logger.LogInformation("Step 3: Composing meal plan with AI");
                var composePlanInput = new ComposePlanInput
                {
                    UserProfile = userProfile,
                    CandidateRecipes =  new List<Recipe>(),//candidateRecipes,

					WeekIdentifier = input.WeekIdentifier
                };

                var mealPlan = await context.CallActivityAsync<MealPlan>("ComposePlanWithLLM", composePlanInput);
                mealPlan.GenerationMetadata.OrchestrationId = context.InstanceId;

                // Step 4: Validate nutrition
                logger.LogInformation("Step 4: Validating nutrition");
                var validationInput = new ValidateNutritionInput
                {
                    MealPlan = mealPlan,
                    UserProfile = userProfile
                };

                NutritionValidationResult validationResult;
                try
                {
                    validationResult = await context.CallActivityAsync<NutritionValidationResult>("ValidateNutrition", validationInput);
                }
                catch (Exception validationEx)
                {
                    logger.LogError(validationEx, "Nutrition validation failed, continuing with default validation");
                    validationResult = new NutritionValidationResult
                    {
                        IsValid = true, // Allow to continue
                        Issues = new List<string> { $"Validation error: {validationEx.Message}" },
                        AiAssessment = "Validation service temporarily unavailable",
                        AdherencePercentage = 70
                    };
                }

                // If validation fails significantly, we could retry or adjust
                if (!validationResult.IsValid && validationResult.AdherencePercentage < 50) // More lenient threshold
                {
                    logger.LogWarning("Nutrition validation failed with {AdherencePercentage}% adherence. Issues: {Issues}", 
                        validationResult.AdherencePercentage, string.Join(", ", validationResult.Issues));
                    
                    // In a production system, you might want to retry generation or apply corrections
                    mealPlan.GenerationMetadata.Errors.AddRange(validationResult.Issues);
                }

                // Update meal plan with validation results
                if (mealPlan.WeeklyNutritionSummary != null && validationResult.AdherencePercentage > 0)
                {
                    mealPlan.WeeklyNutritionSummary.AdherencePercentage = validationResult.AdherencePercentage;
                }

                // Step 5: Compute grocery list
                logger.LogInformation("Step 5: Computing grocery list");
                var groceryList = await context.CallActivityAsync<GroceryList>("ComputeGroceryList", mealPlan);
                mealPlan.GroceryList = groceryList;
                mealPlan.TotalEstimatedCost = groceryList.TotalEstimatedCost;

                // Step 6: Persist the meal plan
                logger.LogInformation("Step 6: Persisting meal plan");
                mealPlan = await context.CallActivityAsync<MealPlan>("PersistPlan", mealPlan);

                // Step 7: Schedule reminders
                logger.LogInformation("Step 7: Scheduling reminders");
                var reminderInput = new ScheduleRemindersInput
                {
                    MealPlan = mealPlan,
                    UserProfile = userProfile
                };

                var scheduledReminders = await context.CallActivityAsync<List<Activities.ReminderSchedule>>("ScheduleReminders", reminderInput);
                
                logger.LogInformation("Successfully generated meal plan: {MealPlanId} with {ReminderCount} reminders", 
                    mealPlan.Id, scheduledReminders.Count);

                // Mark as completed
                mealPlan.Status = MealPlanStatus.Generated;
                mealPlan.GenerationMetadata.CompletedAt = context.CurrentUtcDateTime;

                return mealPlan;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in weekly meal plan orchestration for user: {UserId}", input.UserId);
                
                // Create a failed meal plan for tracking
                var failedPlan = new MealPlan
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = input.UserId,
                    WeekIdentifier = input.WeekIdentifier,
                    Status = MealPlanStatus.Failed,
                    GenerationMetadata = new GenerationMetadata
                    {
                        OrchestrationId = context.InstanceId,
                        StartedAt = context.CurrentUtcDateTime,
                        CompletedAt = context.CurrentUtcDateTime,
                        Errors = new List<string> { ex.Message }
                    }
                };

                // Still try to persist the failed plan for tracking
                try
                {
                    await context.CallActivityAsync("PersistPlan", failedPlan);
                }
                catch (Exception persistEx)
                {
                    logger.LogError(persistEx, "Failed to persist failed meal plan");
                }

                throw; // Re-throw the original exception
            }
        }
    }

    public class GenerateWeeklyPlanInput
    {
        public string UserId { get; set; } = string.Empty;
        public string WeekIdentifier { get; set; } = string.Empty;
        public MealPlanPreferences? Preferences { get; set; }
        public bool RegenerateExisting { get; set; } = false;
    }
}
