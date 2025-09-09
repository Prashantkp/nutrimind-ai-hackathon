using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using NutriMind.Api.Models;
using NutriMind.Api.Services;

namespace NutriMind.Api.Activities
{
    public class RetrieveCandidateRecipesActivity
    {
        private readonly ILogger<RetrieveCandidateRecipesActivity> _logger;
        private readonly ISearchService _searchService;

        public RetrieveCandidateRecipesActivity(
            ILogger<RetrieveCandidateRecipesActivity> logger,
            ISearchService searchService)
        {
            _logger = logger;
            _searchService = searchService;
        }

        [Function("RetrieveCandidateRecipes")]
        public async Task<List<Recipe>> Run([ActivityTrigger] RetrieveRecipesInput input)
        {
            try
            {
                _logger.LogInformation("Retrieving candidate recipes for user with dietary preference: {DietaryPreference}", 
                    input.DietaryPreference);

                var recipes = await _searchService.GetRecipeRecommendationsAsync(
                    input.DietaryPreference,
                    input.Allergens,
                    input.Dislikes,
                    input.MaxRecipes
                );

                // Additional filtering based on user preferences
                var filteredRecipes = recipes.Where(recipe => 
                {
                    // Filter by cooking time preference
                    if (input.MaxCookingTime.HasValue && recipe.TotalTime > input.MaxCookingTime.Value)
                        return false;

                    // Filter by calorie range (recipes shouldn't be too high calorie for a single meal)
                    if (input.MaxCaloriesPerMeal.HasValue && recipe.Calories > input.MaxCaloriesPerMeal.Value)
                        return false;

                    // Filter by preferred cuisines if specified
                    if (input.PreferredCuisines.Any() && 
                        !input.PreferredCuisines.Any(pc => recipe.Cuisine.Contains(pc, StringComparison.OrdinalIgnoreCase)))
                        return false;

                    return true;
                }).ToList();

                _logger.LogInformation("Retrieved {RecipeCount} candidate recipes after filtering", 
                    filteredRecipes.Count);

                return filteredRecipes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving candidate recipes");
                throw;
            }
        }
    }

    public class RetrieveRecipesInput
    {
        public string DietaryPreference { get; set; } = string.Empty;
        public List<string> Allergens { get; set; } = new();
        public List<string> Dislikes { get; set; } = new();
        public List<string> PreferredCuisines { get; set; } = new();
        public int MaxRecipes { get; set; } = 50;
        public int? MaxCookingTime { get; set; }
        public int? MaxCaloriesPerMeal { get; set; }
    }
}
