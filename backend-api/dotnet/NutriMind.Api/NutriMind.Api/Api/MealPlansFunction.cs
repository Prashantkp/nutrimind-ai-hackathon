using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.DurableTask.Client;
using NutriMind.Api.Models;
using NutriMind.Api.Services;
using NutriMind.Api.Orchestrations;
using NutriMind.Api.Helpers;
using System.Net;
using Newtonsoft.Json;

namespace NutriMind.Api.Api
{
    public class MealPlansFunction
    {
        private readonly ILogger<MealPlansFunction> _logger;
        private readonly IMealPlanService _mealPlanService;
        private readonly IAuthService _authService;
        private readonly string _containerName = "meal-plans";

        public MealPlansFunction(
            ILogger<MealPlansFunction> logger,
            IMealPlanService mealPlanService,
            IAuthService authService)
        {
            _logger = logger;
            _mealPlanService = mealPlanService;
            _authService = authService;
        }

        [Function("GenerateMealPlan")]
        public async Task<HttpResponseData> GenerateMealPlan(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "mealplans")] HttpRequestData req,
            [DurableClient] DurableTaskClient client)
        {
            try
            {
                _logger.LogInformation("Starting meal plan generation");

                // Get user ID from authentication
                var userId = _authService.GetUserIdFromRequest(req);
                if (string.IsNullOrEmpty(userId))
                {
                    return await CreateErrorResponse(req, HttpStatusCode.Unauthorized, "Authentication required");
                }

                // Read request body
                var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var generateRequest = JsonConvert.DeserializeObject<GenerateMealPlanRequest>(requestBody);

                if (generateRequest == null || string.IsNullOrEmpty(generateRequest.WeekIdentifier))
                {
                    return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Invalid request body or missing week identifier");
                }

                // Validate week identifier format
                if (!WeekHelper.IsValidWeekIdentifier(generateRequest.WeekIdentifier))
                {
                    return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Invalid week identifier format. Expected YYYY-Www");
                }

                // Check if meal plan already exists (unless regeneration is requested)
                if (!generateRequest.RegenerateExisting)
                {
                    var existingPlan = await GetExistingMealPlan(userId, generateRequest.WeekIdentifier);
                    if (existingPlan != null)
                    {
                        return await CreateSuccessResponse(req, new MealPlanGenerationResponse
                        {
                            OrchestrationId = existingPlan.GenerationMetadata.OrchestrationId,
                            Status = existingPlan.Status.ToString(),
                            EstimatedCompletionTime = DateTime.UtcNow,
                            StatusCheckUrl = $"/api/mealplans/{existingPlan.Id}"
                        }, "Meal plan already exists for this week");
                    }
                }

                // Create orchestration input
                var orchestrationInput = new GenerateWeeklyPlanInput
                {
                    UserId = userId,
                    WeekIdentifier = generateRequest.WeekIdentifier,
                    RegenerateExisting = generateRequest.RegenerateExisting
                };

                // Apply request preferences if provided
                if (generateRequest.DietaryPreference != null || 
                    generateRequest.Allergens != null || 
                    generateRequest.TargetCalories.HasValue)
                {
                    orchestrationInput.Preferences = new MealPlanPreferences
                    {
                        DietaryPreference = generateRequest.DietaryPreference ?? string.Empty,
                        Allergens = generateRequest.Allergens ?? new List<string>(),
                        Dislikes = generateRequest.Dislikes ?? new List<string>(),
                        TargetCalories = generateRequest.TargetCalories ?? 0,
                        MaxPrepTime = generateRequest.MaxPrepTime ?? 30,
                        BudgetConstraint = generateRequest.BudgetConstraint ?? 0,
                        VarietyLevel = generateRequest.VarietyLevel
                    };
                }

                // Start the orchestration
                var instanceId = await client.ScheduleNewOrchestrationInstanceAsync(
                    "GenerateWeeklyPlan",
                    orchestrationInput);

                _logger.LogInformation("Started meal plan generation orchestration: {InstanceId}", instanceId);

                var response = new MealPlanGenerationResponse
                {
                    OrchestrationId = instanceId,
                    Status = "Running",
                    EstimatedCompletionTime = DateTime.UtcNow.AddMinutes(2), // Estimated 2 minutes
                    StatusCheckUrl = $"/api/mealplans/status/{instanceId}"
                };

                return await CreateSuccessResponse(req, response, "Meal plan generation started");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting meal plan generation");
                return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "Internal server error");
            }
        }

        [Function("GetMealPlan")]
        public async Task<HttpResponseData> GetMealPlan(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "mealplans/{id}")] HttpRequestData req,
            string id)
        {
            try
            {
                _logger.LogInformation("Getting meal plan: {MealPlanId}", id);

                // Get user ID from authentication
                var userId = _authService.GetUserIdFromRequest(req);
                if (string.IsNullOrEmpty(userId))
                {
                    return await CreateErrorResponse(req, HttpStatusCode.Unauthorized, "Authentication required");
                }

                // Get meal plan using MealPlanService
                var mealPlan = await _mealPlanService.GetMealPlanAsync(userId, id);
                if (mealPlan == null)
                {
                    return await CreateErrorResponse(req, HttpStatusCode.NotFound, "Meal plan not found");
                }

                return await CreateSuccessResponse(req, mealPlan, "Meal plan retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting meal plan: {MealPlanId}", id);
                return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "Internal server error");
            }
        }

        [Function("GetMealPlansByWeek")]
        public async Task<HttpResponseData> GetMealPlansByWeek(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "mealplans")] HttpRequestData req)
        {
            try
            {
                _logger.LogInformation("Getting meal plans by week");

                // Get user ID from authentication
                var userId = _authService.GetUserIdFromRequest(req);
                if (string.IsNullOrEmpty(userId))
                {
                    return await CreateErrorResponse(req, HttpStatusCode.Unauthorized, "Authentication required");
                }

                // Get week parameter
                var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
                var weekIdentifier = query["week"];

                if (string.IsNullOrEmpty(weekIdentifier))
                {
                    return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Week parameter is required (format: YYYY-Www)");
                }

                if (!WeekHelper.IsValidWeekIdentifier(weekIdentifier))
                {
                    return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Invalid week identifier format. Expected YYYY-Www");
                }

                // Query meal plans for the specific week using MealPlanService
                var mealPlans = await _mealPlanService.GetMealPlansForWeekAsync(userId, weekIdentifier);

                return await CreateSuccessResponse(req, mealPlans, $"Found {mealPlans.Count} meal plans for week {weekIdentifier}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting meal plans by week");
                return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "Internal server error");
            }
        }

        [Function("GetMealPlanStatus")]
        public async Task<HttpResponseData> GetMealPlanStatus(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "mealplans/status/{orchestrationId}")] HttpRequestData req,
            [DurableClient] DurableTaskClient client,
            string orchestrationId)
        {
            try
            {
                _logger.LogInformation("Getting meal plan generation status: {OrchestrationId}", orchestrationId);

                // Get orchestration status
                var status = await client.GetInstanceAsync(orchestrationId);

                if (status == null)
                {
                    return await CreateErrorResponse(req, HttpStatusCode.NotFound, "Orchestration not found");
                }

                var response = new
                {
                    orchestrationId = orchestrationId,
                    status = status.RuntimeStatus.ToString(),
                    createdAt = status.CreatedAt,
                    lastUpdatedAt = status.LastUpdatedAt,
                    output = status.SerializedOutput,
                    customStatus = status.SerializedCustomStatus
                };

                return await CreateSuccessResponse(req, response, "Status retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting orchestration status: {OrchestrationId}", orchestrationId);
                return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "Internal server error");
            }
        }

        [Function("UpdateMealPlan")]
        public async Task<HttpResponseData> UpdateMealPlan(
            [HttpTrigger(AuthorizationLevel.Function, "put", Route = "mealplans/{id}")] HttpRequestData req,
            string id)
        {
            try
            {
                _logger.LogInformation("Updating meal plan: {MealPlanId}", id);

                // Get user ID from authentication
                var userId = _authService.GetUserIdFromRequest(req);
                if (string.IsNullOrEmpty(userId))
                {
                    return await CreateErrorResponse(req, HttpStatusCode.Unauthorized, "Authentication required");
                }

                // Read request body
                var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var updateData = JsonConvert.DeserializeObject<dynamic>(requestBody);

                if (updateData == null)
                {
                    return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Invalid request body");
                }

                // Get existing meal plan using MealPlanService
                var mealPlan = await _mealPlanService.GetMealPlanAsync(userId, id);
                if (mealPlan == null)
                {
                    return await CreateErrorResponse(req, HttpStatusCode.NotFound, "Meal plan not found");
                }

                // Update meal plan (this is a simplified example - you'd want more specific update logic)
                mealPlan.UpdatedAt = DateTime.UtcNow;

                // Save updated meal plan using MealPlanService
                await _mealPlanService.UpdateMealPlanAsync(mealPlan);

                return await CreateSuccessResponse(req, mealPlan, "Meal plan updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating meal plan: {MealPlanId}", id);
                return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "Internal server error");
            }
        }

        private async Task<MealPlan?> GetExistingMealPlan(string userId, string weekIdentifier)
        {
            try
            {
                var mealPlans = await _mealPlanService.GetMealPlansForWeekAsync(userId, weekIdentifier);
                return mealPlans.FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking for existing meal plan");
                return null;
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
