using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using NutriMind.Api.Models;
using NutriMind.Api.Services;
using System.Net;
using Newtonsoft.Json;

namespace NutriMind.Api.Api
{
    public class RecipesFunction
    {
        private readonly ILogger<RecipesFunction> _logger;
        private readonly ISearchService _searchService;
        private readonly IAuthService _authService;

        public RecipesFunction(
            ILogger<RecipesFunction> logger,
            ISearchService searchService,
            IAuthService authService)
        {
            _logger = logger;
            _searchService = searchService;
            _authService = authService;
        }

        [Function("SearchRecipes")]
        public async Task<HttpResponseData> SearchRecipes(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "recipes/search")] HttpRequestData req)
        {
            try
            {
                _logger.LogInformation("Searching recipes");

                // Get user ID from authentication (optional for search)
                var userId = _authService.GetUserIdFromRequest(req);

                // Parse query parameters
                var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
                var searchQuery = query["q"] ?? "";
                var pageSize = int.TryParse(query["pageSize"], out var ps) ? ps : 20;
                var pageNumber = int.TryParse(query["page"], out var pn) ? pn : 1;

                // Parse filters
                var filters = new Dictionary<string, object>();
                
                if (bool.TryParse(query["isVegan"], out var isVegan))
                    filters["isvegan"] = isVegan;
                
                if (bool.TryParse(query["isKeto"], out var isKeto))
                    filters["isketo"] = isKeto;
                
                if (bool.TryParse(query["isDiabeticFriendly"], out var isDiabetic))
                    filters["isdiabeticfriendly"] = isDiabetic;
                
                if (int.TryParse(query["maxCalories"], out var maxCalories))
                    filters["maxcalories"] = maxCalories;
                
                if (!string.IsNullOrEmpty(query["cuisine"]))
                    filters["cuisine"] = query["cuisine"];

                // Search recipes
                var recipes = await _searchService.SearchRecipesAsync(searchQuery, filters, pageSize);

                var response = new RecipeSearchResponse
                {
                    Recipes = recipes,
                    TotalCount = recipes.Count,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    Filters = filters
                };

                return await CreateSuccessResponse(req, response, "Recipes retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching recipes");
                return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "Internal server error");
            }
        }

        [Function("GetRecipe")]
        public async Task<HttpResponseData> GetRecipe(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "recipes/{id}")] HttpRequestData req,
            string id)
        {
            try
            {
                _logger.LogInformation("Getting recipe: {RecipeId}", id);

                // Get recipe by ID
                var recipe = await _searchService.GetRecipeByIdAsync(id);

                if (recipe == null)
                {
                    return await CreateErrorResponse(req, HttpStatusCode.NotFound, "Recipe not found");
                }

                return await CreateSuccessResponse(req, recipe, "Recipe retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recipe: {RecipeId}", id);
                return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "Internal server error");
            }
        }

        [Function("GetRecipeRecommendations")]
        public async Task<HttpResponseData> GetRecipeRecommendations(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "recipes/recommendations")] HttpRequestData req)
        {
            try
            {
                _logger.LogInformation("Getting recipe recommendations");

                // Get user ID from authentication
                var userId = _authService.GetUserIdFromRequest(req);
                if (string.IsNullOrEmpty(userId))
                {
                    return await CreateErrorResponse(req, HttpStatusCode.Unauthorized, "Authentication required");
                }

                // Parse query parameters
                var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
                var dietaryPreference = query["dietaryPreference"] ?? "";
                var maxResults = int.TryParse(query["maxResults"], out var mr) ? mr : 20;
                
                // Parse allergens and dislikes from comma-separated values
                var allergens = !string.IsNullOrEmpty(query["allergens"]) 
                    ? query["allergens"].Split(',').Select(a => a.Trim()).ToList()
                    : new List<string>();
                
                var dislikes = !string.IsNullOrEmpty(query["dislikes"])
                    ? query["dislikes"].Split(',').Select(d => d.Trim()).ToList()
                    : new List<string>();

                // Get recommendations
                var recipes = await _searchService.GetRecipeRecommendationsAsync(
                    dietaryPreference, 
                    allergens, 
                    dislikes, 
                    maxResults);

                var response = new RecipeSearchResponse
                {
                    Recipes = recipes,
                    TotalCount = recipes.Count,
                    PageNumber = 1,
                    PageSize = maxResults,
                    Filters = new Dictionary<string, object>
                    {
                        ["dietaryPreference"] = dietaryPreference,
                        ["allergens"] = allergens,
                        ["dislikes"] = dislikes
                    }
                };

                return await CreateSuccessResponse(req, response, "Recipe recommendations retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recipe recommendations");
                return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "Internal server error");
            }
        }

        private async Task<HttpResponseData> CreateSuccessResponse<T>(HttpRequestData req, T data, string message)
        {
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(ApiResponse<T>.SuccessResponse(data, message));
            return response;
        }

        private async Task<HttpResponseData> CreateErrorResponse(HttpRequestData req, HttpStatusCode statusCode, string message)
        {
            var response = req.CreateResponse(statusCode);
            await response.WriteAsJsonAsync(ApiResponse<object>.ErrorResponse(message));
            return response;
        }
    }
}
