using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using NutriMind.Api.Models;
using NutriMind.Api.Services;
using System.Net;
using Newtonsoft.Json;

namespace NutriMind.Api.Api
{
    public class ProfileFunction
    {
        private readonly ILogger<ProfileFunction> _logger;
        private readonly IUserProfileService _userProfileService;
        private readonly IAuthService _authService;

        public ProfileFunction(
            ILogger<ProfileFunction> logger, 
            IUserProfileService userProfileService,
            IAuthService authService)
        {
            _logger = logger;
            _userProfileService = userProfileService;
            _authService = authService;
        }

        [Function("CreateProfile")]
        public async Task<HttpResponseData> CreateProfile(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "profile")] HttpRequestData req)
        {
            try
            {
                _logger.LogInformation("Creating user profile");

                // Get authenticated user ID from token
                var userId = _authService.GetUserIdFromRequest(req);
                if (string.IsNullOrEmpty(userId))
                {
                    return await CreateErrorResponse(req, HttpStatusCode.Unauthorized, "Authentication required");
                }

                // Read request body
                var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var createRequest = JsonConvert.DeserializeObject<CreateUserProfileRequest>(requestBody);

                if (createRequest == null)
                {
                    return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Invalid request body");
                }

                // Check if user profile already exists
                var existingProfile = await _userProfileService.GetUserProfileAsync(userId);
                if (existingProfile != null)
                {
                    return await CreateErrorResponse(req, HttpStatusCode.Conflict, "User profile already exists");
                }

                // Create user profile
                var userProfile = new UserProfile
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = userId,
                    Email = createRequest.Email,
                    FirstName = createRequest.FirstName,
                    LastName = createRequest.LastName,
                    Age = createRequest.Age,
                    Height = createRequest.Height,
                    Weight = createRequest.Weight,
                    ActivityLevel = createRequest.ActivityLevel,
                    DietaryPreference = createRequest.DietaryPreference,
                    Allergens = createRequest.Allergens,
                    Dislikes = createRequest.Dislikes,
                    HealthGoals = createRequest.HealthGoals,
                    TargetCalories = createRequest.TargetCalories,
                    MealFrequency = createRequest.MealFrequency,
                    CookingSkillLevel = createRequest.CookingSkillLevel,
                    CookingTimePreference = createRequest.CookingTimePreference,
                    BudgetPerWeek = createRequest.BudgetPerWeek,
                    PreferredCuisines = createRequest.PreferredCuisines,
                    NotificationPreferences = createRequest.NotificationPreferences,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                // Calculate target macros if not provided
                if (userProfile.TargetProtein == 0)
                    userProfile.TargetProtein = CalculateProteinTarget(userProfile.Weight, userProfile.ActivityLevel);
                if (userProfile.TargetCarbs == 0)
                    userProfile.TargetCarbs = CalculateCarbTarget(userProfile.TargetCalories);
                if (userProfile.TargetFats == 0)
                    userProfile.TargetFats = CalculateFatTarget(userProfile.TargetCalories);

                // Save profile using service
                var savedProfile = await _userProfileService.CreateUserProfileAsync(userProfile);

                // Generate JWT token
                var token = _authService.GenerateToken(userId, userProfile.Email);

                var result = new
                {
                    user = savedProfile,
                    token = token
                };

                return await CreateSuccessResponse(req, result, "Profile created successfully");
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
            {
                return await CreateErrorResponse(req, HttpStatusCode.Conflict, "User profile already exists");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user profile");
                return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "Internal server error");
            }
        }

        [Function("GetProfile")]
        public async Task<HttpResponseData> GetProfile(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "profile")] HttpRequestData req)
        {
            try
            {
                _logger.LogInformation("Getting user profile");

                // Get user ID from authentication
                var userId = _authService.GetUserIdFromRequest(req);
                if (string.IsNullOrEmpty(userId))
                {
                    return await CreateErrorResponse(req, HttpStatusCode.Unauthorized, "Authentication required");
                }

                // Get user profile using service
                var userProfile = await _userProfileService.GetUserProfileAsync(userId);
                if (userProfile == null)
                {
                    return await CreateErrorResponse(req, HttpStatusCode.NotFound, "User profile not found");
                }

                return await CreateSuccessResponse(req, userProfile, "Profile retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user profile");
                return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "Internal server error");
            }
        }

        [Function("UpdateProfile")]
        public async Task<HttpResponseData> UpdateProfile(
            [HttpTrigger(AuthorizationLevel.Function, "put", Route = "profile")] HttpRequestData req)
        {
            try
            {
                _logger.LogInformation("Updating user profile");

                // Get user ID from authentication
                var userId = _authService.GetUserIdFromRequest(req);
                if (string.IsNullOrEmpty(userId))
                {
                    return await CreateErrorResponse(req, HttpStatusCode.Unauthorized, "Authentication required");
                }

                // Read request body
                var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var updateRequest = JsonConvert.DeserializeObject<UpdateUserProfileRequest>(requestBody);

                if (updateRequest == null)
                {
                    return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Invalid request body");
                }

                // Get existing profile using service
                var userProfile = await _userProfileService.GetUserProfileAsync(userId);
                if (userProfile == null)
                {
                    return await CreateErrorResponse(req, HttpStatusCode.NotFound, "User profile not found");
                }

                // Update only provided fields
                if (!string.IsNullOrEmpty(updateRequest.FirstName))
                    userProfile.FirstName = updateRequest.FirstName;
                if (!string.IsNullOrEmpty(updateRequest.LastName))
                    userProfile.LastName = updateRequest.LastName;
                if (updateRequest.Age.HasValue)
                    userProfile.Age = updateRequest.Age.Value;
                if (updateRequest.Height.HasValue)
                    userProfile.Height = updateRequest.Height.Value;
                if (updateRequest.Weight.HasValue)
                    userProfile.Weight = updateRequest.Weight.Value;
                if (!string.IsNullOrEmpty(updateRequest.ActivityLevel))
                    userProfile.ActivityLevel = updateRequest.ActivityLevel;
                if (!string.IsNullOrEmpty(updateRequest.DietaryPreference))
                    userProfile.DietaryPreference = updateRequest.DietaryPreference;
                if (updateRequest.Allergens != null)
                    userProfile.Allergens = updateRequest.Allergens;
                if (updateRequest.Dislikes != null)
                    userProfile.Dislikes = updateRequest.Dislikes;
                if (updateRequest.HealthGoals != null)
                    userProfile.HealthGoals = updateRequest.HealthGoals;
                if (updateRequest.TargetCalories.HasValue)
                    userProfile.TargetCalories = updateRequest.TargetCalories.Value;
                if (updateRequest.MealFrequency.HasValue)
                    userProfile.MealFrequency = updateRequest.MealFrequency.Value;
                if (!string.IsNullOrEmpty(updateRequest.CookingSkillLevel))
                    userProfile.CookingSkillLevel = updateRequest.CookingSkillLevel;
                if (updateRequest.CookingTimePreference.HasValue)
                    userProfile.CookingTimePreference = updateRequest.CookingTimePreference.Value;
                if (updateRequest.BudgetPerWeek.HasValue)
                    userProfile.BudgetPerWeek = updateRequest.BudgetPerWeek.Value;
                if (updateRequest.PreferredCuisines != null)
                    userProfile.PreferredCuisines = updateRequest.PreferredCuisines;
                if (updateRequest.NotificationPreferences != null)
                    userProfile.NotificationPreferences = updateRequest.NotificationPreferences;

                userProfile.UpdatedAt = DateTime.UtcNow;

                // Save updated profile using service
                var updatedProfile = await _userProfileService.UpdateUserProfileAsync(userProfile);

                return await CreateSuccessResponse(req, updatedProfile, "Profile updated successfully");
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
            {
                return await CreateErrorResponse(req, HttpStatusCode.NotFound, "User profile not found");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user profile");
                return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "Internal server error");
            }
        }

        private int CalculateProteinTarget(float weight, string activityLevel)
        {
            // Basic protein calculation: 0.8-1.2g per kg body weight
            var multiplier = activityLevel.ToLower() switch
            {
                "sedentary" => 0.8f,
                "light" => 1.0f,
                "moderate" => 1.2f,
                "active" => 1.4f,
                "very_active" => 1.6f,
                _ => 1.0f
            };

            return (int)(weight * multiplier);
        }

        private int CalculateCarbTarget(int targetCalories)
        {
            // Carbs should be 45-65% of total calories, using 50%
            return (int)(targetCalories * 0.5 / 4); // 4 calories per gram of carbs
        }

        private int CalculateFatTarget(int targetCalories)
        {
            // Fats should be 20-35% of total calories, using 30%
            return (int)(targetCalories * 0.3 / 9); // 9 calories per gram of fat
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
