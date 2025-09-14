using Azure;
using Azure.Identity;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NutriMind.Api.Models;
using System.Text.Json;

namespace NutriMind.Api.Services
{
    public interface ISearchService
    {
        Task<List<Recipe>> SearchRecipesAsync(string query, Dictionary<string, object>? filters = null, int maxResults = 20);
        Task<List<Recipe>> GetRecipeRecommendationsAsync(string dietaryPreference, List<string> allergens, List<string> dislikes, int maxResults = 50, int maxCaloriesPerMeal = 500);
        Task<Recipe?> GetRecipeByIdAsync(string recipeId);
    }

    public class SearchService : ISearchService
    {
        private readonly SearchClient _searchClient;
        private readonly ILogger<SearchService> _logger;

        public SearchService(IConfiguration configuration, ILogger<SearchService> logger)
        {
            _logger = logger;
			var keyVaultUri = configuration["KeyVaultUri"];
			var kvClient = new SecretClient(new Uri(keyVaultUri), new DefaultAzureCredential());
			var endpoint = configuration["SearchEndpoint"];
            var apiKey = kvClient.GetSecret("ai-search-key").Value.Value;
            var indexName = configuration["AzureSearchIndex"] ?? "reciepe-index-new";

            if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(apiKey))
            {
                _logger.LogWarning("Azure Search configuration is missing. Search functionality will be limited.");
                // In production, you might want to throw an exception here
                // For development, we'll create a mock client
                _searchClient = null!;
                return;
            }

            _searchClient = new SearchClient(new Uri(endpoint), indexName, new AzureKeyCredential(apiKey));
        }

        public async Task<List<Recipe>> SearchRecipesAsync(string query, Dictionary<string, object>? filters = null, int maxResults = 20)
        {
            if (_searchClient == null)
            {
                _logger.LogWarning("Search client not configured, returning mock data");
                return GetMockRecipes().Take(maxResults).ToList();
            }

            try
            {
                var searchOptions = new SearchOptions
                {
                    Size = maxResults,
                    IncludeTotalCount = true
                };

                // Add filters
                if (filters != null)
                {
                    var filterExpressions = new List<string>();

                    foreach (var filter in filters)
                    {
                        switch (filter.Key.ToLower())
                        {
                            case "isvegan":
                                if (filter.Value is bool isVegan && isVegan)
                                    filterExpressions.Add("is_vegan eq true");
                                break;
                            case "isketo":
                                if (filter.Value is bool isKeto && isKeto)
                                    filterExpressions.Add("is_keto eq true");
                                break;
                            case "isdiabeticfriendly":
                                if (filter.Value is bool isDiabetic && isDiabetic)
                                    filterExpressions.Add("is_diabetic_friendly eq true");
                                break;
                            case "maxcalories":
                                if (filter.Value is int maxCal)
                                    filterExpressions.Add($"calories le {maxCal}");
                                break;
                            case "cuisine":
                                if (filter.Value is string cuisine)
                                    filterExpressions.Add($"cuisine eq '{cuisine}'");
                                break;
                        }
                    }

                    if (filterExpressions.Any())
                        searchOptions.Filter = string.Join(" and ", filterExpressions);
                }

				//var response = await _searchClient.SearchAsync<Recipe>(query, searchOptions);
				var response = await _searchClient.SearchAsync<Recipe>("*", searchOptions);
				var recipes = new List<Recipe>();

                await foreach (var result in response.Value.GetResultsAsync())
                {
                    recipes.Add(result.Document);
                }

                return recipes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching recipes with query: {Query}", query);
                return GetMockRecipes().Take(maxResults).ToList();
            }
        }

        public async Task<List<Recipe>> GetRecipeRecommendationsAsync(string dietaryPreference, List<string> allergens, List<string> dislikes, int maxResults = 50, int maxCaloriesPerMeal = 500)
        {
			var filters = new Dictionary<string, object>();
			var filterExpressions = new List<string>();

			// Add dietary preference filters - FIX: Use correct property names
			switch (dietaryPreference.ToLower())
			{
				case "vegan":
				case "vegetarian":
					filterExpressions.Add("isVegan eq true");  // Changed from is_vegan
					break;
				case "keto":
					filterExpressions.Add("isKeto eq true");   // Changed from is_keto
					break;
				case "diabetic-friendly":
					filterExpressions.Add("isDiabeticFriendly eq true");  // Changed from is_diabetic_friendly
					break;
			}

			filterExpressions.Add($"calories ge {maxCaloriesPerMeal - 100} and calories le {maxCaloriesPerMeal + 100}");

			// For allergens and dislikes, we'd need more complex filtering
			var searchQuery = "*"; // Search all recipes initially

			//if (allergens != null)
			//{
			//	foreach (var allergen in allergens)
			//	{
			//		var escapedAllergen = allergen.Replace("'", "''");
			//		filterExpressions.Add($"not ingredients/any(ingredient: ingredient/item eq '{escapedAllergen}')");
			//	}
			//}

			//if (dislikes != null)
			//{
			//	foreach (var dislike in dislikes)
			//	{
			//		var escapedDislike = dislike.Replace("'", "''");
			//		filterExpressions.Add($"not ingredients/any(ingredient: ingredient/item eq '{escapedDislike}')");
			//	}
			//}

			var searchOptions = new SearchOptions
			{
				Size = maxResults,
				IncludeTotalCount = true
			};

			if (filterExpressions.Any())
				searchOptions.Filter = string.Join(" and ", filterExpressions);

			var response = await _searchClient.SearchAsync<SearchDocument>("*", searchOptions);
			var recipes = new List<SearchDocument>();

			await foreach (var result in response.Value.GetResultsAsync())
			{
				recipes.Add(result.Document);
			}

            return new List<Recipe>();
			//return recipes;
		}

        public async Task<Recipe?> GetRecipeByIdAsync(string recipeId)
        {
            if (_searchClient == null)
            {
                _logger.LogWarning("Search client not configured, returning mock data");
                return GetMockRecipes().FirstOrDefault(r => r.Id == recipeId);
            }

            try
            {
                var response = await _searchClient.GetDocumentAsync<Recipe>(recipeId);
                return response.Value;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recipe by ID: {RecipeId}", recipeId);
                return null;
            }
        }

        private List<Recipe> GetMockRecipes()
        {
            return new List<Recipe>
            {
                new Recipe
                {
                    Id = "recipe-1",
                    Name = "Quinoa Buddha Bowl",
                    Cuisine = "healthy",
                    IsVegan = true,
                    IsKeto = false,
                    IsDiabeticFriendly = true,
                    Calories = 450,
                    Tags = new List<string> { "healthy", "quinoa", "vegetables", "plant-based" },
                    Ingredients = new List<Ingredient>
                    {
                        new() { Item = "quinoa", Qty = "1", Unit = "cup", Category = "grains", EstimatedCost = 3 },
                        new() { Item = "kale", Qty = "2", Unit = "cups", Category = "produce", EstimatedCost = 2 },
                        new() { Item = "chickpeas", Qty = "1", Unit = "can", Category = "pantry", EstimatedCost = 1 }
                    },
                    Steps = new List<string>
                    {
                        "Cook quinoa according to package instructions",
                        "Massage kale with olive oil",
                        "Drain and rinse chickpeas",
                        "Combine all ingredients in a bowl"
                    },
                    Difficulty = "easy",
                    TotalTime = 25
                },
                new Recipe
                {
                    Id = "recipe-2", 
                    Name = "Keto Salmon with Asparagus",
                    Cuisine = "keto",
                    IsVegan = false,
                    IsKeto = true,
                    IsDiabeticFriendly = true,
                    Calories = 380,
                    Tags = new List<string> { "keto", "low-carb", "salmon", "omega-3" },
                    Ingredients = new List<Ingredient>
                    {
                        new() { Item = "salmon fillet", Qty = "6", Unit = "oz", Category = "protein", EstimatedCost = 8 },
                        new() { Item = "asparagus", Qty = "1", Unit = "bunch", Category = "produce", EstimatedCost = 3 },
                        new() { Item = "olive oil", Qty = "2", Unit = "tbsp", Category = "pantry", EstimatedCost = 0 }
                    },
                    Steps = new List<string>
                    {
                        "Preheat oven to 400°F",
                        "Season salmon with salt and pepper",
                        "Toss asparagus with olive oil",
                        "Bake for 15-18 minutes"
                    },
                    Difficulty = "easy",
                    TotalTime = 20
                }
            };
        }
    }
}
