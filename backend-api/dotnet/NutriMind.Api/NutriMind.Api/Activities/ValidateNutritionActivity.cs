using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using NutriMind.Api.Models;
using NutriMind.Api.Services;
using NutriMind.Api.Helpers;

namespace NutriMind.Api.Activities
{
    public class ValidateNutritionActivity
    {
        private readonly ILogger<ValidateNutritionActivity> _logger;
        private readonly IOpenAIService _openAIService;

        public ValidateNutritionActivity(
            ILogger<ValidateNutritionActivity> logger,
            IOpenAIService openAIService)
        {
            _logger = logger;
            _openAIService = openAIService;
        }

        [Function("ValidateNutrition")]
        public async Task<NutritionValidationResult> Run([ActivityTrigger] ValidateNutritionInput input)
        {
            try
            {
                _logger.LogInformation("Validating nutrition for meal plan: {MealPlanId}", input.MealPlan.Id);

                // Calculate comprehensive nutrition summary
                CalculateNutritionSummary(input.MealPlan);

                // Use AI to validate the nutritional adequacy
                var aiValidation = await _openAIService.ValidateNutritionAsync(input.MealPlan, input.UserProfile);

                // Perform rule-based validation
                var validationIssues = PerformRuleBasedValidation(input.MealPlan, input.UserProfile);

                var result = new NutritionValidationResult
                {
                    IsValid = !validationIssues.Any(),
                    Issues = validationIssues,
                    AiAssessment = aiValidation,
                    NutritionSummary = input.MealPlan.WeeklyNutritionSummary,
                    AdherencePercentage = CalculateAdherencePercentage(input.MealPlan, input.UserProfile)
                };

                _logger.LogInformation("Nutrition validation completed for meal plan: {MealPlanId}, Valid: {IsValid}, Issues: {IssueCount}", 
                    input.MealPlan.Id, result.IsValid, result.Issues.Count);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating nutrition for meal plan: {MealPlanId}", input.MealPlan.Id);
                throw;
            }
        }

        private void CalculateNutritionSummary(MealPlan mealPlan)
        {
            int totalCalories = 0;
            decimal totalProtein = 0;
            decimal totalCarbs = 0;
            decimal totalFats = 0;

            foreach (var dailyPlan in mealPlan.DailyMeals.Values)
            {
                int dailyCalories = 0;
                decimal dailyProtein = 0;
                decimal dailyCarbs = 0;
                decimal dailyFats = 0;

                // Calculate nutrition for regular meals
                foreach (var meal in dailyPlan.Meals.Values)
                {
                    var nutrition = meal.Nutrition;
                    dailyCalories += nutrition.Calories * meal.Servings;
                    dailyProtein += nutrition.Protein * meal.Servings;
                    dailyCarbs += nutrition.Carbohydrates * meal.Servings;
                    dailyFats += nutrition.Fats * meal.Servings;
                }

                // Calculate nutrition for snacks
                foreach (var snack in dailyPlan.Snacks)
                {
                    var nutrition = snack.Nutrition;
                    dailyCalories += nutrition.Calories * snack.Servings;
                    dailyProtein += nutrition.Protein * snack.Servings;
                    dailyCarbs += nutrition.Carbohydrates * snack.Servings;
                    dailyFats += nutrition.Fats * snack.Servings;
                }

                // Update daily summary
                dailyPlan.DailyNutrition = new NutritionSummary
                {
                    TotalCalories = dailyCalories,
                    TotalProtein = dailyProtein,
                    TotalCarbs = dailyCarbs,
                    TotalFats = dailyFats,
                    AverageDailyCalories = dailyCalories
                };

                // Add to weekly totals
                totalCalories += dailyCalories;
                totalProtein += dailyProtein;
                totalCarbs += dailyCarbs;
                totalFats += dailyFats;
            }

            // Update weekly summary
            mealPlan.WeeklyNutritionSummary = new NutritionSummary
            {
                TotalCalories = totalCalories,
                TotalProtein = totalProtein,
                TotalCarbs = totalCarbs,
                TotalFats = totalFats,
                AverageDailyCalories = mealPlan.DailyMeals.Any() ? totalCalories / mealPlan.DailyMeals.Count : 0
            };
        }

        private List<string> PerformRuleBasedValidation(MealPlan mealPlan, UserProfile userProfile)
        {
            var issues = new List<string>();

            var avgDailyCalories = mealPlan.WeeklyNutritionSummary.AverageDailyCalories;
            var targetCalories = userProfile.TargetCalories;

            // Calorie validation (allow 10% variance)
            var calorieVariance = Math.Abs(avgDailyCalories - targetCalories) / (double)targetCalories;
            if (calorieVariance > 0.15)
            {
                if (avgDailyCalories < targetCalories * 0.85)
                {
                    issues.Add("Daily calorie intake is significantly below target. Consider adding healthy snacks or larger portions.");
                }
                else if (avgDailyCalories > targetCalories * 1.15)
                {
                    issues.Add("Daily calorie intake is significantly above target. Consider smaller portions or lower-calorie alternatives.");
                }
            }

            // Protein validation (should be 10-35% of calories)
            var avgDailyProtein = mealPlan.WeeklyNutritionSummary.TotalProtein / (mealPlan.DailyMeals.Count > 0 ? mealPlan.DailyMeals.Count : 1);
            var proteinCaloriesPercentage = (double)(avgDailyProtein * 4) / avgDailyCalories;
            if (proteinCaloriesPercentage < 0.10)
            {
                issues.Add("Protein intake is below recommended levels. Consider adding more protein-rich foods.");
            }
            else if (proteinCaloriesPercentage > 0.35)
            {
                issues.Add("Protein intake is above recommended levels. Consider balancing with more carbohydrates and healthy fats.");
            }

            // Check for dietary preference compliance
            if (!string.IsNullOrEmpty(userProfile.DietaryPreference))
            {
                var complianceIssues = CheckDietaryCompliance(mealPlan, userProfile.DietaryPreference);
                issues.AddRange(complianceIssues);
            }

            // Check for allergen exposure
            if (userProfile.Allergens.Any())
            {
                var allergenIssues = CheckAllergenExposure(mealPlan, userProfile.Allergens);
                issues.AddRange(allergenIssues);
            }

            // Check meal frequency
            if (userProfile.MealFrequency > 0)
            {
                var avgMealsPerDay = mealPlan.DailyMeals.Values.Average(d => d.Meals.Count + d.Snacks.Count);
                if (avgMealsPerDay < userProfile.MealFrequency - 1)
                {
                    issues.Add("Meal frequency is below your preference. Consider adding more meals or snacks throughout the day.");
                }
            }

            return issues;
        }

        private List<string> CheckDietaryCompliance(MealPlan mealPlan, string dietaryPreference)
        {
            var issues = new List<string>();

            foreach (var dailyPlan in mealPlan.DailyMeals.Values)
            {
                var allMeals = dailyPlan.Meals.Values.ToList();
                allMeals.AddRange(dailyPlan.Snacks);

                foreach (var meal in allMeals)
                {
                    var recipe = meal.Recipe;
                    var isCompliant = dietaryPreference.ToLower() switch
                    {
                        "vegan" => recipe.IsVegan,
                        "keto" => recipe.IsKeto,
                        "diabetic-friendly" => recipe.IsDiabeticFriendly,
                        _ => true // Unknown preference, assume compliant
                    };

                    if (!isCompliant)
                    {
                        issues.Add($"Recipe '{recipe.Name}' may not comply with {dietaryPreference} dietary preference.");
                    }
                }
            }

            return issues.Distinct().ToList();
        }

        private List<string> CheckAllergenExposure(MealPlan mealPlan, List<string> allergens)
        {
            var issues = new List<string>();

            foreach (var dailyPlan in mealPlan.DailyMeals.Values)
            {
                var allMeals = dailyPlan.Meals.Values.ToList();
                allMeals.AddRange(dailyPlan.Snacks);

                foreach (var meal in allMeals)
                {
                    foreach (var ingredient in meal.Recipe.Ingredients)
                    {
                        foreach (var allergen in allergens)
                        {
                            if (ingredient.Item.Contains(allergen, StringComparison.OrdinalIgnoreCase))
                            {
                                issues.Add($"Recipe '{meal.Recipe.Name}' contains potential allergen: {allergen}");
                            }
                        }
                    }
                }
            }

            return issues.Distinct().ToList();
        }

        private decimal CalculateAdherencePercentage(MealPlan mealPlan, UserProfile userProfile)
        {
            var scores = new List<decimal>();

            // Calorie adherence
            var avgDailyCalories = mealPlan.WeeklyNutritionSummary.AverageDailyCalories;
            var targetCalories = userProfile.TargetCalories;
            if (targetCalories > 0)
            {
                var calorieScore = Math.Max(0, 100 - Math.Abs(avgDailyCalories - targetCalories) / (decimal)targetCalories * 100);
                scores.Add(calorieScore);
            }

            // Protein adherence (if target is set)
            if (userProfile.TargetProtein > 0)
            {
                var avgProtein = mealPlan.WeeklyNutritionSummary.TotalProtein / (mealPlan.DailyMeals.Count > 0 ? mealPlan.DailyMeals.Count : 1);
                var proteinScore = Math.Max(0, 100 - Math.Abs(avgProtein - userProfile.TargetProtein) / userProfile.TargetProtein * 100);
                scores.Add(proteinScore);
            }

            // Dietary preference adherence (simplified scoring)
            if (!string.IsNullOrEmpty(userProfile.DietaryPreference))
            {
                var complianceIssues = CheckDietaryCompliance(mealPlan, userProfile.DietaryPreference);
                var totalMeals = mealPlan.DailyMeals.Values.Sum(d => d.Meals.Count + d.Snacks.Count);
                var complianceScore = totalMeals > 0 ? (decimal)(totalMeals - complianceIssues.Count) / totalMeals * 100 : 100;
                scores.Add(Math.Max(0, complianceScore));
            }

            return scores.Any() ? scores.Average() : 85; // Default to 85% if no scores calculated
        }
    }

    public class ValidateNutritionInput
    {
        public MealPlan MealPlan { get; set; } = new();
        public UserProfile UserProfile { get; set; } = new();
    }

    public class NutritionValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Issues { get; set; } = new();
        public string AiAssessment { get; set; } = string.Empty;
        public NutritionSummary NutritionSummary { get; set; } = new();
        public decimal AdherencePercentage { get; set; }
    }
}
