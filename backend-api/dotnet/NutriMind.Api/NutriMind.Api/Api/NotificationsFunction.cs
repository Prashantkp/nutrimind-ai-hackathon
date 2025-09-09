using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using NutriMind.Api.Models;
using NutriMind.Api.Services;
using System.Net;
using System.Text;
using Newtonsoft.Json;

namespace NutriMind.Api.Api
{
    public class NotificationsFunction
    {
        private readonly ILogger<NotificationsFunction> _logger;
        private readonly INotificationService _notificationService;
        private readonly IAuthService _authService;
        private readonly IUserProfileService _userProfileService;
        private readonly IMealPlanService _mealPlanService;

        public NotificationsFunction(
            ILogger<NotificationsFunction> logger,
            INotificationService notificationService,
            IAuthService authService,
            IUserProfileService userProfileService,
            IMealPlanService mealPlanService)
        {
            _logger = logger;
            _notificationService = notificationService;
            _authService = authService;
            _userProfileService = userProfileService;
            _mealPlanService = mealPlanService;
        }

        [Function("TestNotification")]
        public async Task<HttpResponseData> TestNotification(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "notifications/test")] HttpRequestData req)
        {
            try
            {
                _logger.LogInformation("Testing notification");

                // Get user ID from authentication
                var userId = _authService.GetUserIdFromRequest(req);
                if (string.IsNullOrEmpty(userId))
                {
                    return await CreateErrorResponse(req, HttpStatusCode.Unauthorized, "Authentication required");
                }

                // Read request body
                var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var testRequest = JsonConvert.DeserializeObject<NotificationTestRequest>(requestBody);

                if (testRequest == null || string.IsNullOrEmpty(testRequest.Type))
                {
                    return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Invalid request body");
                }

                // Get user profile for notification preferences
                var userProfile = await GetUserProfile(userId);
                if (userProfile == null)
                {
                    return await CreateErrorResponse(req, HttpStatusCode.NotFound, "User profile not found");
                }

                var result = await SendTestNotification(testRequest.Type, userProfile, testRequest);

                return await CreateSuccessResponse(req, result, "Test notification sent successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing notification");
                return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "Internal server error");
            }
        }

        [Function("SendMealReminder")]
        public async Task<HttpResponseData> SendMealReminder(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "notifications/meal-reminder")] HttpRequestData req)
        {
            try
            {
                _logger.LogInformation("Sending meal reminder");

                // Read request body
                var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var reminderRequest = JsonConvert.DeserializeObject<MealReminderRequest>(requestBody);

                if (reminderRequest == null || string.IsNullOrEmpty(reminderRequest.UserId) || string.IsNullOrEmpty(reminderRequest.MealType))
                {
                    return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Invalid request body");
                }

                // Get user profile
                var userProfile = await GetUserProfile(reminderRequest.UserId);
                if (userProfile == null)
                {
                    return await CreateErrorResponse(req, HttpStatusCode.NotFound, "User profile not found");
                }

                var result = await SendMealReminder(userProfile, reminderRequest);

                return await CreateSuccessResponse(req, result, "Meal reminder sent successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending meal reminder");
                return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "Internal server error");
            }
        }

        [Function("SendGroceryReminder")]
        public async Task<HttpResponseData> SendGroceryReminder(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "notifications/grocery-reminder")] HttpRequestData req)
        {
            try
            {
                _logger.LogInformation("Sending grocery reminder");

                // Read request body
                var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var groceryRequest = JsonConvert.DeserializeObject<GroceryReminderRequest>(requestBody);

                if (groceryRequest == null || string.IsNullOrEmpty(groceryRequest.UserId) || string.IsNullOrEmpty(groceryRequest.MealPlanId))
                {
                    return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Invalid request body");
                }

                // Get user profile
                var userProfile = await GetUserProfile(groceryRequest.UserId);
                if (userProfile == null)
                {
                    return await CreateErrorResponse(req, HttpStatusCode.NotFound, "User profile not found");
                }

                // Get meal plan
                var mealPlan = await GetMealPlan(groceryRequest.UserId, groceryRequest.MealPlanId);
                if (mealPlan == null)
                {
                    return await CreateErrorResponse(req, HttpStatusCode.NotFound, "Meal plan not found");
                }

                var result = await SendGroceryReminder(userProfile, mealPlan, groceryRequest);

                return await CreateSuccessResponse(req, result, "Grocery reminder sent successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending grocery reminder");
                return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "Internal server error");
            }
        }

        [Function("GetNotificationSettings")]
        public async Task<HttpResponseData> GetNotificationSettings(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "notifications/settings")] HttpRequestData req)
        {
            try
            {
                _logger.LogInformation("Getting notification settings");

                // Get user ID from authentication
                var userId = _authService.GetUserIdFromRequest(req);
                if (string.IsNullOrEmpty(userId))
                {
                    return await CreateErrorResponse(req, HttpStatusCode.Unauthorized, "Authentication required");
                }

                // Get user profile
                var userProfile = await GetUserProfile(userId);
                if (userProfile == null)
                {
                    return await CreateErrorResponse(req, HttpStatusCode.NotFound, "User profile not found");
                }

                var settings = new NotificationSettings
                {
                    EmailNotifications = userProfile.Preferences?.Notifications?.EmailEnabled ?? true,
                    PushNotifications = userProfile.Preferences?.Notifications?.PushEnabled ?? true,
                    MealReminders = userProfile.Preferences?.Notifications?.MealReminders ?? true,
                    GroceryReminders = userProfile.Preferences?.Notifications?.GroceryReminders ?? true,
                    WeeklyPlanReminders = userProfile.Preferences?.Notifications?.WeeklyPlanReminders ?? true,
                    NutritionTips = userProfile.Preferences?.Notifications?.NutritionTips ?? false,
                    ReminderTimes = userProfile.Preferences?.Notifications?.ReminderTimes ?? new List<string> { "09:00", "12:00", "18:00" }
                };

                return await CreateSuccessResponse(req, settings, "Notification settings retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting notification settings");
                return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "Internal server error");
            }
        }

        [Function("UpdateNotificationSettings")]
        public async Task<HttpResponseData> UpdateNotificationSettings(
            [HttpTrigger(AuthorizationLevel.Function, "put", Route = "notifications/settings")] HttpRequestData req)
        {
            try
            {
                _logger.LogInformation("Updating notification settings");

                // Get user ID from authentication
                var userId = _authService.GetUserIdFromRequest(req);
                if (string.IsNullOrEmpty(userId))
                {
                    return await CreateErrorResponse(req, HttpStatusCode.Unauthorized, "Authentication required");
                }

                // Read request body
                var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var settings = JsonConvert.DeserializeObject<NotificationSettings>(requestBody);

                if (settings == null)
                {
                    return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Invalid notification settings");
                }

                // Get user profile
                var userProfile = await GetUserProfile(userId);
                if (userProfile == null)
                {
                    return await CreateErrorResponse(req, HttpStatusCode.NotFound, "User profile not found");
                }

                // Update notification preferences
                if (userProfile.Preferences == null)
                    userProfile.Preferences = new UserPreferences();

                if (userProfile.Preferences.Notifications == null)
                    userProfile.Preferences.Notifications = new NotificationPreferences();

                userProfile.Preferences.Notifications.EmailEnabled = settings.EmailNotifications;
                userProfile.Preferences.Notifications.PushEnabled = settings.PushNotifications;
                userProfile.Preferences.Notifications.MealReminders = settings.MealReminders;
                userProfile.Preferences.Notifications.GroceryReminders = settings.GroceryReminders;
                userProfile.Preferences.Notifications.WeeklyPlanReminders = settings.WeeklyPlanReminders;
                userProfile.Preferences.Notifications.NutritionTips = settings.NutritionTips;
                userProfile.Preferences.Notifications.ReminderTimes = settings.ReminderTimes;

                userProfile.UpdatedAt = DateTime.UtcNow;

                // Save updated profile using UserProfileService
                await _userProfileService.UpdateUserProfileAsync(userProfile);

                return await CreateSuccessResponse(req, settings, "Notification settings updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating notification settings");
                return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "Internal server error");
            }
        }

        private async Task<UserProfile?> GetUserProfile(string userId)
        {
            try
            {
                return await _userProfileService.GetUserProfileAsync(userId);
            }
            catch (Exception)
            {
                return null;
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

        private async Task<NotificationResult> SendTestNotification(string type, UserProfile userProfile, NotificationTestRequest request)
        {
            try
            {
                var result = new NotificationResult
                {
                    Type = type,
                    Recipient = userProfile.Email,
                    SentAt = DateTime.UtcNow,
                    Success = true
                };

                switch (type.ToLower())
                {
                    case "email":
                        result = await SendTestEmail(userProfile, request.Message);
                        break;
                    case "push":
                        result = await SendTestPushNotification(userProfile, request.Message);
                        break;
                    case "sms":
                        result = await SendTestSMSNotification(userProfile, request.Message);
                        break;
                    default:
                        result.Success = false;
                        result.ErrorMessage = "Unsupported notification type";
                        break;
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending test notification of type {Type}", type);
                return new NotificationResult
                {
                    Type = type,
                    Success = false,
                    ErrorMessage = ex.Message,
                    SentAt = DateTime.UtcNow
                };
            }
        }

        private async Task<NotificationResult> SendTestEmail(UserProfile userProfile, string? customMessage = null)
        {
            // Mock email sending - in production, use Azure Communication Services or SendGrid
            _logger.LogInformation("Sending test email to {Email}", userProfile.Email);

            var subject = "NutriMind - Test Notification";
            var message = customMessage ?? "This is a test notification from NutriMind. Your notification settings are working correctly!";

            // Simulate email sending delay
            await Task.Delay(100);

            return new NotificationResult
            {
                Type = "email",
                Recipient = userProfile.Email,
                Subject = subject,
                Message = message,
                Success = true,
                SentAt = DateTime.UtcNow
            };
        }

        private async Task<NotificationResult> SendTestPushNotification(UserProfile userProfile, string? customMessage = null)
        {
            // Mock push notification - in production, use Azure Notification Hubs or Firebase
            _logger.LogInformation("Sending test push notification to user {UserId}", userProfile.Id);

            var message = customMessage ?? "Test notification from NutriMind!";

            // Simulate push notification delay
            await Task.Delay(50);

            return new NotificationResult
            {
                Type = "push",
                Recipient = userProfile.Id,
                Message = message,
                Success = true,
                SentAt = DateTime.UtcNow
            };
        }

        private async Task<NotificationResult> SendTestSMSNotification(UserProfile userProfile, string? customMessage = null)
        {
            // Mock SMS sending - in production, use Azure Communication Services or Twilio
            _logger.LogInformation("Sending test SMS to user {UserId}", userProfile.Id);

            var message = customMessage ?? "Test SMS from NutriMind - your notifications are working!";

            // Simulate SMS sending delay
            await Task.Delay(200);

            return new NotificationResult
            {
                Type = "sms",
                Recipient = userProfile.PhoneNumber ?? "Not provided",
                Message = message,
                Success = !string.IsNullOrEmpty(userProfile.PhoneNumber),
                ErrorMessage = string.IsNullOrEmpty(userProfile.PhoneNumber) ? "Phone number not provided" : null,
                SentAt = DateTime.UtcNow
            };
        }

        private async Task<NotificationResult> SendMealReminder(UserProfile userProfile, MealReminderRequest request)
        {
            var mealType = request.MealType.ToLower();
            var subject = $"NutriMind - {char.ToUpper(mealType[0]) + mealType[1..]} Reminder";
            var message = request.CustomMessage ?? $"Time for your {mealType}! Check your NutriMind meal plan for today's delicious options.";

            if (request.RecipeName != null)
            {
                message += $" Today's featured {mealType}: {request.RecipeName}";
            }

            // Send based on user preferences
            var results = new List<NotificationResult>();

            if (userProfile.Preferences?.Notifications?.EmailEnabled == true)
            {
                results.Add(await SendTestEmail(userProfile, message));
            }

            if (userProfile.Preferences?.Notifications?.PushEnabled == true)
            {
                results.Add(await SendTestPushNotification(userProfile, message));
            }

            // Return the first successful result or the first result if none succeeded
            var successfulResult = results.FirstOrDefault(r => r.Success) ?? results.FirstOrDefault();
            
            return successfulResult ?? new NotificationResult
            {
                Type = "meal_reminder",
                Success = false,
                ErrorMessage = "No notification methods enabled",
                SentAt = DateTime.UtcNow
            };
        }

        private async Task<NotificationResult> SendGroceryReminder(UserProfile userProfile, MealPlan mealPlan, GroceryReminderRequest request)
        {
            var subject = "NutriMind - Grocery Shopping Reminder";
            var itemCount = mealPlan.GroceryList?.Items?.Count ?? 0;
            var message = request.CustomMessage ?? $"Don't forget to shop for your meal plan! You have {itemCount} items on your grocery list for the week of {mealPlan.WeekStartDate:MMM dd}.";

            if (request.SuggestedStore != null)
            {
                message += $" Suggested store: {request.SuggestedStore}";
            }

            // Send based on user preferences
            var results = new List<NotificationResult>();

            if (userProfile.Preferences?.Notifications?.EmailEnabled == true && userProfile.Preferences?.Notifications?.GroceryReminders == true)
            {
                results.Add(await SendTestEmail(userProfile, message));
            }

            if (userProfile.Preferences?.Notifications?.PushEnabled == true && userProfile.Preferences?.Notifications?.GroceryReminders == true)
            {
                results.Add(await SendTestPushNotification(userProfile, message));
            }

            // Return the first successful result or the first result if none succeeded
            var successfulResult = results.FirstOrDefault(r => r.Success) ?? results.FirstOrDefault();
            
            return successfulResult ?? new NotificationResult
            {
                Type = "grocery_reminder",
                Success = false,
                ErrorMessage = "No notification methods enabled or grocery reminders disabled",
                SentAt = DateTime.UtcNow
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
