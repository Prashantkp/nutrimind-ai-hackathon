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
                _logger.LogInformation("Validating nutrition for meal plan: {MealPlanId}", input.MealPlan?.Id ?? "unknown");

                // Ensure we have a valid meal plan
                if (input.MealPlan?.DailyMeals == null || !input.MealPlan.DailyMeals.Any())
                {
                    _logger.LogWarning("Meal plan has no daily meals to validate");
                    return new NutritionValidationResult
                    {
                        IsValid = false,
                        Issues = new List<string> { "Meal plan contains no daily meals" },
                        AiAssessment = "No meals to assess",
                        AdherencePercentage = 0
                    };
                }

                // Calculate comprehensive nutrition summary with null safety
                CalculateNutritionSummary(input.MealPlan);

                // Use AI to validate the nutritional adequacy (with fallback)
                string aiValidation;
                try 
                {
                    aiValidation = await _openAIService.ValidateNutritionAsync(input.MealPlan, input.UserProfile);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "AI validation failed, using fallback assessment");
                    aiValidation = "AI validation temporarily unavailable - using rule-based validation only.";
                }

                // Perform rule-based validation with error handling
                var validationIssues = PerformRuleBasedValidation(input.MealPlan, input.UserProfile);

                var result = new NutritionValidationResult
                {
                    IsValid = validationIssues.Count <= 2, // Allow minor issues
                    Issues = validationIssues,
                    AiAssessment = aiValidation,
                    NutritionSummary = input.MealPlan.WeeklyNutritionSummary,
                    AdherencePercentage = CalculateAdherencePercentage(input.MealPlan, input.UserProfile)
                };

                _logger.LogInformation("Nutrition validation completed for meal plan: {MealPlanId}, Valid: {IsValid}, Issues: {IssueCount}, Adherence: {Adherence}%", 
                    input.MealPlan.Id, result.IsValid, result.Issues.Count, result.AdherencePercentage);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating nutrition for meal plan: {MealPlanId}", input.MealPlan?.Id ?? "unknown");
                
                // Return a safe fallback result instead of throwing
                return new NutritionValidationResult
                {
                    IsValid = false,
                    Issues = new List<string> { $"Validation error: {ex.Message}" },
                    AiAssessment = "Validation failed due to technical error",
                    AdherencePercentage = 50 // Conservative estimate
                };
            }
        }

        private void CalculateNutritionSummary(MealPlan mealPlan)
        {
            int totalCalories = 0;
            decimal totalProtein = 0;
            decimal totalCarbs = 0;
            decimal totalFats = 0;

            if (mealPlan?.DailyMeals == null)
            {
                _logger.LogWarning("Meal plan has no daily meals to calculate nutrition");
                mealPlan.WeeklyNutritionSummary = new NutritionSummary();
                return;
            }

            foreach (var dailyPlan in mealPlan.DailyMeals.Values)
            {
                if (dailyPlan?.Meals == null) continue;

                int dailyCalories = 0;
                decimal dailyProtein = 0;
                decimal dailyCarbs = 0;
                decimal dailyFats = 0;

                // Calculate nutrition for regular meals
                foreach (var meal in dailyPlan.Meals.Values)
                {
                    if (meal?.Nutrition == null) continue;
                    
                    var nutrition = meal.Nutrition;
                    var servings = Math.Max(1, meal.Servings); // Ensure at least 1 serving
                    
                    dailyCalories += nutrition.Calories * servings;
                    dailyProtein += nutrition.Protein * servings;
                    dailyCarbs += nutrition.Carbohydrates * servings;
                    dailyFats += nutrition.Fats * servings;
                }

                // Calculate nutrition for snacks (if they exist)
                if (dailyPlan.Snacks != null)
                {
                    foreach (var snack in dailyPlan.Snacks)
                    {
                        if (snack?.Nutrition == null) continue;

                        var nutrition = snack.Nutrition;
                        var servings = Math.Max(1, snack.Servings);
                        
                        dailyCalories += nutrition.Calories * servings;
                        dailyProtein += nutrition.Protein * servings;
                        dailyCarbs += nutrition.Carbohydrates * servings;
                        dailyFats += nutrition.Fats * servings;
                    }
                }

                // Initialize daily nutrition if null
                dailyPlan.DailyNutrition ??= new NutritionSummary();

                // Update daily summary
                dailyPlan.DailyNutrition.TotalCalories = dailyCalories;
                dailyPlan.DailyNutrition.TotalProtein = dailyProtein;
                dailyPlan.DailyNutrition.TotalCarbs = dailyCarbs;
                dailyPlan.DailyNutrition.TotalFats = dailyFats;
                dailyPlan.DailyNutrition.AverageDailyCalories = dailyCalories;

                // Add to weekly totals
                totalCalories += dailyCalories;
                totalProtein += dailyProtein;
                totalCarbs += dailyCarbs;
                totalFats += dailyFats;
            }

            // Initialize weekly summary if null
            mealPlan.WeeklyNutritionSummary ??= new NutritionSummary();

            // Update weekly summary
            mealPlan.WeeklyNutritionSummary.TotalCalories = totalCalories;
            mealPlan.WeeklyNutritionSummary.TotalProtein = totalProtein;
            mealPlan.WeeklyNutritionSummary.TotalCarbs = totalCarbs;
            mealPlan.WeeklyNutritionSummary.TotalFats = totalFats;
            
            var dayCount = Math.Max(1, mealPlan.DailyMeals.Count); // Avoid division by zero
            mealPlan.WeeklyNutritionSummary.AverageDailyCalories = totalCalories / dayCount;
            
            _logger.LogInformation("Calculated nutrition for {DayCount} days: {TotalCals} total calories, {AvgCals} avg daily", 
                dayCount, totalCalories, mealPlan.WeeklyNutritionSummary.AverageDailyCalories);
        }

        private List<string> PerformRuleBasedValidation(MealPlan mealPlan, UserProfile userProfile)
        {
            var issues = new List<string>();

            try
            {
                if (mealPlan?.WeeklyNutritionSummary == null)
                {
                    issues.Add("No nutrition summary available for validation");
                    return issues;
                }

                var avgDailyCalories = mealPlan.WeeklyNutritionSummary.AverageDailyCalories;
                var targetCalories = userProfile?.TargetCalories ?? 2000; // Default if not set

                // Calorie validation (allow 20% variance for partial meal plans)
                if (avgDailyCalories > 0 && targetCalories > 0)
                {
                    var calorieVariance = Math.Abs(avgDailyCalories - targetCalories) / (double)targetCalories;
                    if (calorieVariance > 0.20) // More lenient for incomplete plans
                    {
                        if (avgDailyCalories < targetCalories * 0.80)
                        {
                            issues.Add("Daily calorie intake may be below target. Consider adding healthy snacks or larger portions.");
                        }
                        else if (avgDailyCalories > targetCalories * 1.20)
                        {
                            issues.Add("Daily calorie intake may be above target. Consider smaller portions or lower-calorie alternatives.");
                        }
                    }
                }

                // Protein validation (should be 10-35% of calories) - only if we have data
                if (mealPlan.DailyMeals.Any() && avgDailyCalories > 0)
                {
                    var avgDailyProtein = mealPlan.WeeklyNutritionSummary.TotalProtein / Math.Max(1, mealPlan.DailyMeals.Count);
                    var proteinCaloriesPercentage = (double)(avgDailyProtein * 4) / avgDailyCalories;
                    
                    if (proteinCaloriesPercentage < 0.10)
                    {
                        issues.Add("Protein intake may be below recommended levels. Consider adding more protein-rich foods.");
                    }
                    else if (proteinCaloriesPercentage > 0.35)
                    {
                        issues.Add("Protein intake may be above recommended levels. Consider balancing with more carbohydrates and healthy fats.");
                    }
                }

                // Check for dietary preference compliance (with null safety)
                if (!string.IsNullOrEmpty(userProfile?.DietaryPreference))
                {
                    var complianceIssues = CheckDietaryCompliance(mealPlan, userProfile.DietaryPreference);
                    issues.AddRange(complianceIssues);
                }

                // Check for allergen exposure (with null safety)
                if (userProfile?.Allergens?.Any() == true)
                {
                    var allergenIssues = CheckAllergenExposure(mealPlan, userProfile.Allergens);
                    issues.AddRange(allergenIssues);
                }

                // Meal frequency check (less strict for partial plans)
                if (userProfile?.MealFrequency > 0 && mealPlan.DailyMeals.Any())
                {
                    var avgMealsPerDay = mealPlan.DailyMeals.Values
                        .Where(d => d?.Meals != null)
                        .Average(d => d.Meals.Count + (d.Snacks?.Count ?? 0));
                    
                    if (avgMealsPerDay < userProfile.MealFrequency - 1)
                    {
                        issues.Add("Meal frequency may be below your preference. Consider adding more meals or snacks throughout the day.");
                    }
                }

                // Add warning if we have incomplete meal plan
                if (mealPlan.DailyMeals.Count < 7)
                {
                    issues.Add($"Meal plan contains only {mealPlan.DailyMeals.Count} days instead of 7. This is a partial plan.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during rule-based validation");
                issues.Add($"Validation incomplete due to error: {ex.Message}");
            }

            return issues;
        }

        private List<string> CheckDietaryCompliance(MealPlan mealPlan, string dietaryPreference)
        {
            var issues = new List<string>();

            try
            {
                if (mealPlan?.DailyMeals == null) return issues;

                foreach (var dailyPlan in mealPlan.DailyMeals.Values)
                {
                    if (dailyPlan?.Meals == null) continue;

                    var allMeals = dailyPlan.Meals.Values.Where(m => m != null).ToList();
                    if (dailyPlan.Snacks != null)
                    {
                        allMeals.AddRange(dailyPlan.Snacks.Where(s => s != null));
                    }

                    foreach (var meal in allMeals)
                    {
                        if (meal?.Recipe == null) continue;

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
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking dietary compliance");
                issues.Add("Unable to verify dietary compliance due to data structure issues");
            }

            return issues.Distinct().ToList();
        }

        private List<string> CheckAllergenExposure(MealPlan mealPlan, List<string> allergens)
        {
            var issues = new List<string>();

            try
            {
                if (mealPlan?.DailyMeals == null || allergens == null || !allergens.Any()) 
                    return issues;

                foreach (var dailyPlan in mealPlan.DailyMeals.Values)
                {
                    if (dailyPlan?.Meals == null) continue;

                    var allMeals = dailyPlan.Meals.Values.Where(m => m != null).ToList();
                    if (dailyPlan.Snacks != null)
                    {
                        allMeals.AddRange(dailyPlan.Snacks.Where(s => s != null));
                    }

                    foreach (var meal in allMeals)
                    {
                        if (meal?.Recipe?.Ingredients == null) continue;

                        foreach (var ingredient in meal.Recipe.Ingredients)
                        {
                            if (string.IsNullOrEmpty(ingredient?.Item)) continue;

                            foreach (var allergen in allergens)
                            {
                                if (ingredient.Item.Contains(allergen, StringComparison.OrdinalIgnoreCase))
                                {
                                    issues.Add($"Recipe '{meal.Recipe.Name}' contains potential allergen: {allergen} (ingredient: {ingredient.Item})");
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking allergen exposure");
                issues.Add("Unable to verify allergen safety due to data structure issues");
            }

            return issues.Distinct().ToList();
        }

        private decimal CalculateAdherencePercentage(MealPlan mealPlan, UserProfile userProfile)
        {
            var scores = new List<decimal>();

            try
            {
                if (mealPlan?.WeeklyNutritionSummary == null || userProfile == null)
                {
                    return 75; // Default middle score if data is missing
                }

                // Calorie adherence
                var avgDailyCalories = mealPlan.WeeklyNutritionSummary.AverageDailyCalories;
                var targetCalories = userProfile.TargetCalories > 0 ? userProfile.TargetCalories : 2000; // Use default if not set
                
                if (avgDailyCalories > 0)
                {
                    var calorieVariance = Math.Abs(avgDailyCalories - targetCalories) / (decimal)targetCalories;
                    var calorieScore = Math.Max(0, 100 - calorieVariance * 100);
                    scores.Add(calorieScore);
                }

                // Protein adherence (if target is set)
                if (userProfile.TargetProtein > 0 && mealPlan.DailyMeals.Count > 0)
                {
                    var avgProtein = mealPlan.WeeklyNutritionSummary.TotalProtein / Math.Max(1, mealPlan.DailyMeals.Count);
                    var proteinVariance = Math.Abs(avgProtein - userProfile.TargetProtein) / userProfile.TargetProtein;
                    var proteinScore = Math.Max(0, 100 - proteinVariance * 100);
                    scores.Add(proteinScore);
                }

                // Dietary preference adherence (simplified scoring)
                if (!string.IsNullOrEmpty(userProfile.DietaryPreference))
                {
                    var complianceIssues = CheckDietaryCompliance(mealPlan, userProfile.DietaryPreference);
                    var totalMeals = mealPlan.DailyMeals.Values
                        .Where(d => d?.Meals != null)
                        .Sum(d => d.Meals.Count + (d.Snacks?.Count ?? 0));
                    
                    var complianceScore = totalMeals > 0 ? (decimal)Math.Max(0, totalMeals - complianceIssues.Count) / totalMeals * 100 : 100;
                    scores.Add(complianceScore);
                }

                // Completeness score (penalize incomplete meal plans)
                var completenessScore = (decimal)mealPlan.DailyMeals.Count / 7 * 100;
                scores.Add(completenessScore);

                return scores.Any() ? Math.Max(0, Math.Min(100, scores.Average())) : 75; // Clamp between 0-100
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating adherence percentage");
                return 60; // Conservative fallback
            }
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
