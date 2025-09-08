using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using NutriMind.Api.Models;
using NutriMind.Api.Services;
using System.Net;
using Newtonsoft.Json;

namespace NutriMind.Api.Api
{
    public class GroceryFunction
    {
        private readonly ILogger<GroceryFunction> _logger;
        private readonly IGroceryService _groceryService;
        private readonly IAuthService _authService;
        private readonly HttpClient _httpClient;
        private readonly IMealPlanService _mealPlanService;

        public GroceryFunction(
            ILogger<GroceryFunction> logger,
            IGroceryService groceryService,
            IAuthService authService,
            HttpClient httpClient,
            IMealPlanService mealPlanService)
        {
            _logger = logger;
            _groceryService = groceryService;
            _authService = authService;
            _httpClient = httpClient;
            _mealPlanService = mealPlanService;
        }

        [Function("CreateGroceryCheckout")]
        public async Task<HttpResponseData> CreateGroceryCheckout(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "grocery/checkout")] HttpRequestData req)
        {
            try
            {
                _logger.LogInformation("Creating grocery checkout");

                // Get user ID from authentication
                var userId = _authService.GetUserIdFromRequest(req);
                if (string.IsNullOrEmpty(userId))
                {
                    return await CreateErrorResponse(req, HttpStatusCode.Unauthorized, "Authentication required");
                }

                // Read request body
                var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var checkoutRequest = JsonConvert.DeserializeObject<GroceryCheckoutRequest>(requestBody);

                if (checkoutRequest == null || string.IsNullOrEmpty(checkoutRequest.MealPlanId) || string.IsNullOrEmpty(checkoutRequest.Provider))
                {
                    return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Invalid request body");
                }

                // Get the meal plan and its grocery list
                var mealPlan = await GetMealPlan(userId, checkoutRequest.MealPlanId);
                if (mealPlan == null)
                {
                    return await CreateErrorResponse(req, HttpStatusCode.NotFound, "Meal plan not found");
                }

                if (mealPlan.GroceryList == null || !mealPlan.GroceryList.Items.Any())
                {
                    return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "No grocery list found for this meal plan");
                }

                // Filter items if specific items were selected
                var itemsToAdd = checkoutRequest.SelectedItemIds?.Any() == true
                    ? mealPlan.GroceryList.Items.Where(item => checkoutRequest.SelectedItemIds.Contains(item.Name)).ToList()
                    : mealPlan.GroceryList.Items;

                // Create checkout based on provider
                var checkoutResponse = await CreateProviderCheckout(
                    checkoutRequest.Provider,
                    itemsToAdd,
                    userId,
                    checkoutRequest.DeliveryDate,
                    checkoutRequest.DeliveryTime);

                if (checkoutResponse == null)
                {
                    return await CreateErrorResponse(req, HttpStatusCode.BadRequest, $"Failed to create checkout with {checkoutRequest.Provider}");
                }

                return await CreateSuccessResponse(req, checkoutResponse, "Checkout created successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating grocery checkout");
                return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "Internal server error");
            }
        }

        [Function("GetGroceryList")]
        public async Task<HttpResponseData> GetGroceryList(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "grocery/{mealPlanId}")] HttpRequestData req,
            string mealPlanId)
        {
            try
            {
                _logger.LogInformation("Getting grocery list for meal plan: {MealPlanId}", mealPlanId);

                // Get user ID from authentication
                var userId = _authService.GetUserIdFromRequest(req);
                if (string.IsNullOrEmpty(userId))
                {
                    return await CreateErrorResponse(req, HttpStatusCode.Unauthorized, "Authentication required");
                }

                // Get the meal plan
                var mealPlan = await GetMealPlan(userId, mealPlanId);
                if (mealPlan == null)
                {
                    return await CreateErrorResponse(req, HttpStatusCode.NotFound, "Meal plan not found");
                }

                if (mealPlan.GroceryList == null)
                {
                    return await CreateErrorResponse(req, HttpStatusCode.NotFound, "No grocery list found for this meal plan");
                }

                return await CreateSuccessResponse(req, mealPlan.GroceryList, "Grocery list retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting grocery list for meal plan: {MealPlanId}", mealPlanId);
                return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "Internal server error");
            }
        }

        [Function("UpdateGroceryList")]
        public async Task<HttpResponseData> UpdateGroceryList(
            [HttpTrigger(AuthorizationLevel.Function, "put", Route = "grocery/{mealPlanId}")] HttpRequestData req,
            string mealPlanId)
        {
            try
            {
                _logger.LogInformation("Updating grocery list for meal plan: {MealPlanId}", mealPlanId);

                // Get user ID from authentication
                var userId = _authService.GetUserIdFromRequest(req);
                if (string.IsNullOrEmpty(userId))
                {
                    return await CreateErrorResponse(req, HttpStatusCode.Unauthorized, "Authentication required");
                }

                // Read request body
                var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var updatedGroceryList = JsonConvert.DeserializeObject<GroceryList>(requestBody);

                if (updatedGroceryList == null)
                {
                    return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Invalid grocery list data");
                }

                // Get the meal plan
                var mealPlan = await GetMealPlan(userId, mealPlanId);
                if (mealPlan == null)
                {
                    return await CreateErrorResponse(req, HttpStatusCode.NotFound, "Meal plan not found");
                }

                // Update the grocery list
                mealPlan.GroceryList = updatedGroceryList;
                mealPlan.UpdatedAt = DateTime.UtcNow;

                // Save updated meal plan using MealPlanService
                await _mealPlanService.UpdateMealPlanAsync(mealPlan);

                return await CreateSuccessResponse(req, updatedGroceryList, "Grocery list updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating grocery list for meal plan: {MealPlanId}", mealPlanId);
                return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "Internal server error");
            }
        }

        [Function("GetAvailableProviders")]
        public async Task<HttpResponseData> GetAvailableProviders(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "grocery/providers")] HttpRequestData req)
        {
            try
            {
                _logger.LogInformation("Getting available grocery providers");

                var providers = new[]
                {
                    new { 
                        id = "instacart", 
                        name = "Instacart", 
                        description = "Same-day grocery delivery from local stores",
                        isAvailable = true,
                        minOrder = 10.00m,
                        deliveryFee = 3.99m
                    },
                    new { 
                        id = "amazon_fresh", 
                        name = "Amazon Fresh", 
                        description = "Fresh groceries delivered by Amazon",
                        isAvailable = true,
                        minOrder = 35.00m,
                        deliveryFee = 0.00m // Free with minimum order
                    },
                    new { 
                        id = "walmart_grocery", 
                        name = "Walmart Grocery", 
                        description = "Grocery pickup and delivery from Walmart",
                        isAvailable = true,
                        minOrder = 35.00m,
                        deliveryFee = 7.95m
                    },
                    new { 
                        id = "kroger", 
                        name = "Kroger", 
                        description = "Fresh groceries from your local Kroger",
                        isAvailable = true,
                        minOrder = 35.00m,
                        deliveryFee = 9.95m
                    }
                };

                return await CreateSuccessResponse(req, providers, "Available providers retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available providers");
                return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "Internal server error");
            }
        }

        private async Task<MealPlan?> GetMealPlan(string userId, string mealPlanId)
        {
            try
            {
                return await _mealPlanService.GetMealPlanAsync(userId, mealPlanId);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private async Task<GroceryCheckoutResponse?> CreateProviderCheckout(
            string provider, 
            List<GroceryItem> items, 
            string userId,
            string? deliveryDate,
            string? deliveryTime)
        {
            try
            {
                // In a real implementation, this would make API calls to the specific grocery provider
                // For demo purposes, we'll create mock responses

                var totalCost = items.Sum(item => item.EstimatedCost);
                var cartId = Guid.NewGuid().ToString();

                return provider.ToLower() switch
                {
                    "instacart" => await CreateInstacartCheckout(items, cartId, totalCost),
                    "amazon_fresh" => await CreateAmazonFreshCheckout(items, cartId, totalCost),
                    "walmart_grocery" => await CreateWalmartCheckout(items, cartId, totalCost),
                    "kroger" => await CreateKrogerCheckout(items, cartId, totalCost),
                    _ => null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating provider checkout for {Provider}", provider);
                return null;
            }
        }

        private async Task<GroceryCheckoutResponse> CreateInstacartCheckout(List<GroceryItem> items, string cartId, decimal totalCost)
        {
            // Mock Instacart checkout
            _logger.LogInformation("Creating Instacart checkout with {ItemCount} items", items.Count);

            return new GroceryCheckoutResponse
            {
                CheckoutUrl = $"https://instacart.com/checkout/{cartId}",
                CartId = cartId,
                Provider = "instacart",
                EstimatedTotal = totalCost + 3.99m, // Add delivery fee
                ItemCount = items.Count,
                ExpiresAt = DateTime.UtcNow.AddHours(24)
            };
        }

        private async Task<GroceryCheckoutResponse> CreateAmazonFreshCheckout(List<GroceryItem> items, string cartId, decimal totalCost)
        {
            // Mock Amazon Fresh checkout
            _logger.LogInformation("Creating Amazon Fresh checkout with {ItemCount} items", items.Count);

            var deliveryFee = totalCost >= 35 ? 0 : 7.99m;

            return new GroceryCheckoutResponse
            {
                CheckoutUrl = $"https://fresh.amazon.com/checkout/{cartId}",
                CartId = cartId,
                Provider = "amazon_fresh",
                EstimatedTotal = totalCost + deliveryFee,
                ItemCount = items.Count,
                ExpiresAt = DateTime.UtcNow.AddHours(24)
            };
        }

        private async Task<GroceryCheckoutResponse> CreateWalmartCheckout(List<GroceryItem> items, string cartId, decimal totalCost)
        {
            // Mock Walmart checkout
            _logger.LogInformation("Creating Walmart checkout with {ItemCount} items", items.Count);

            return new GroceryCheckoutResponse
            {
                CheckoutUrl = $"https://grocery.walmart.com/checkout/{cartId}",
                CartId = cartId,
                Provider = "walmart_grocery",
                EstimatedTotal = totalCost + 7.95m,
                ItemCount = items.Count,
                ExpiresAt = DateTime.UtcNow.AddHours(24)
            };
        }

        private async Task<GroceryCheckoutResponse> CreateKrogerCheckout(List<GroceryItem> items, string cartId, decimal totalCost)
        {
            // Mock Kroger checkout
            _logger.LogInformation("Creating Kroger checkout with {ItemCount} items", items.Count);

            return new GroceryCheckoutResponse
            {
                CheckoutUrl = $"https://www.kroger.com/checkout/{cartId}",
                CartId = cartId,
                Provider = "kroger",
                EstimatedTotal = totalCost + 9.95m,
                ItemCount = items.Count,
                ExpiresAt = DateTime.UtcNow.AddHours(24)
            };
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
