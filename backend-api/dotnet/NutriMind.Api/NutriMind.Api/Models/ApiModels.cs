using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace NutriMind.Api.Models
{
    // Request DTOs
    public class CreateUserProfileRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        public string LastName { get; set; } = string.Empty;

        [Range(1, 150)]
        public int Age { get; set; }

        public float Height { get; set; }
        public float Weight { get; set; }
        public string ActivityLevel { get; set; } = string.Empty;
        public string DietaryPreference { get; set; } = string.Empty;
        public List<string> Allergens { get; set; } = new();
        public List<string> Dislikes { get; set; } = new();
        public List<string> HealthGoals { get; set; } = new();
        public int TargetCalories { get; set; }
        public int MealFrequency { get; set; } = 3;
        public string CookingSkillLevel { get; set; } = "beginner";
        public int CookingTimePreference { get; set; } = 30;
        public decimal BudgetPerWeek { get; set; }
        public List<string> PreferredCuisines { get; set; } = new();
        public NotificationPreferences NotificationPreferences { get; set; } = new();
    }

    public class UpdateUserProfileRequest
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public int? Age { get; set; }
        public float? Height { get; set; }
        public float? Weight { get; set; }
        public string? ActivityLevel { get; set; }
        public string? DietaryPreference { get; set; }
        public List<string>? Allergens { get; set; }
        public List<string>? Dislikes { get; set; }
        public List<string>? HealthGoals { get; set; }
        public int? TargetCalories { get; set; }
        public int? MealFrequency { get; set; }
        public string? CookingSkillLevel { get; set; }
        public int? CookingTimePreference { get; set; }
        public decimal? BudgetPerWeek { get; set; }
        public List<string>? PreferredCuisines { get; set; }
        public NotificationPreferences? NotificationPreferences { get; set; }
    }

    public class GenerateMealPlanRequest
    {
        [Required]
        public string WeekIdentifier { get; set; } = string.Empty; // YYYY-Www format

        public string? DietaryPreference { get; set; }
        public List<string>? Allergens { get; set; }
        public List<string>? Dislikes { get; set; }
        public int? TargetCalories { get; set; }
        public int? MaxPrepTime { get; set; }
        public decimal? BudgetConstraint { get; set; }
        public string VarietyLevel { get; set; } = "medium";
        public bool RegenerateExisting { get; set; } = false;
    }

    public class ConnectProviderRequest
    {
        [Required]
        public string Provider { get; set; } = string.Empty;

        [Required]
        public string RedirectUri { get; set; } = string.Empty;
    }

    public class OAuthCallbackRequest
    {
        [Required]
        public string Provider { get; set; } = string.Empty;

        [Required]
        public string Code { get; set; } = string.Empty;

        [Required]
        public string State { get; set; } = string.Empty;
    }

    public class GroceryCheckoutRequest
    {
        [Required]
        public string MealPlanId { get; set; } = string.Empty;

        [Required]
        public string Provider { get; set; } = string.Empty; // instacart, amazon_fresh, etc.

        public List<string>? SelectedItemIds { get; set; } // optional: specific items to add to cart
        public string? DeliveryDate { get; set; }
        public string? DeliveryTime { get; set; }
    }

    public class SendTestNotificationRequest
    {
        [Required]
        public string NotificationType { get; set; } = string.Empty; // meal_reminder, shopping_reminder, etc.

        public string? CustomMessage { get; set; }
        public Dictionary<string, object>? Parameters { get; set; }
    }

    public class NotificationTestRequest
    {
        [Required]
        public string Type { get; set; } = string.Empty; // "email", "push", "sms"
        public string? Message { get; set; }
    }

    public class MealReminderRequest
    {
        [Required]
        public string UserId { get; set; } = string.Empty;
        [Required]
        public string MealType { get; set; } = string.Empty; // "breakfast", "lunch", "dinner", "snack"
        public string? RecipeName { get; set; }
        public string? CustomMessage { get; set; }
        public DateTime? ScheduledTime { get; set; }
    }

    public class GroceryReminderRequest
    {
        [Required]
        public string UserId { get; set; } = string.Empty;
        [Required]
        public string MealPlanId { get; set; } = string.Empty;
        public string? CustomMessage { get; set; }
        public string? SuggestedStore { get; set; }
        public DateTime? ReminderTime { get; set; }
    }

    public class NotificationResult
    {
        public string Type { get; set; } = string.Empty;
        public string? Recipient { get; set; }
        public string? Subject { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime SentAt { get; set; }
    }

    public class NotificationSettings
    {
        public bool EmailNotifications { get; set; } = true;
        public bool PushNotifications { get; set; } = true;
        public bool MealReminders { get; set; } = true;
        public bool GroceryReminders { get; set; } = true;
        public bool WeeklyPlanReminders { get; set; } = true;
        public bool NutritionTips { get; set; } = false;
        public List<string> ReminderTimes { get; set; } = new List<string>();
    }

    // Response DTOs
    public class ApiResponse<T>
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("data")]
        public T? Data { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; } = string.Empty;

        [JsonProperty("errors")]
        public List<string> Errors { get; set; } = new();

        [JsonProperty("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public static ApiResponse<T> SuccessResponse(T data, string message = "Success")
        {
            return new ApiResponse<T>
            {
                Success = true,
                Data = data,
                Message = message
            };
        }

        public static ApiResponse<T> ErrorResponse(string message, List<string>? errors = null)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = message,
                Errors = errors ?? new List<string>()
            };
        }
    }

    public class MealPlanGenerationResponse
    {
        [JsonProperty("orchestrationId")]
        public string OrchestrationId { get; set; } = string.Empty;

        [JsonProperty("status")]
        public string Status { get; set; } = string.Empty;

        [JsonProperty("estimatedCompletionTime")]
        public DateTime EstimatedCompletionTime { get; set; }

        [JsonProperty("statusCheckUrl")]
        public string StatusCheckUrl { get; set; } = string.Empty;
    }

    public class OAuthInitiationResponse
    {
        [JsonProperty("authorizationUrl")]
        public string AuthorizationUrl { get; set; } = string.Empty;

        [JsonProperty("state")]
        public string State { get; set; } = string.Empty;
    }

    public class GroceryCheckoutResponse
    {
        [JsonProperty("checkoutUrl")]
        public string CheckoutUrl { get; set; } = string.Empty;

        [JsonProperty("cartId")]
        public string CartId { get; set; } = string.Empty;

        [JsonProperty("provider")]
        public string Provider { get; set; } = string.Empty;

        [JsonProperty("estimatedTotal")]
        public decimal EstimatedTotal { get; set; }

        [JsonProperty("itemCount")]
        public int ItemCount { get; set; }

        [JsonProperty("expiresAt")]
        public DateTime ExpiresAt { get; set; }
    }

    public class NotificationTestResponse
    {
        [JsonProperty("sent")]
        public bool Sent { get; set; }

        [JsonProperty("notificationId")]
        public string NotificationId { get; set; } = string.Empty;

        [JsonProperty("message")]
        public string Message { get; set; } = string.Empty;

        [JsonProperty("sentAt")]
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
    }

    public class RecipeSearchResponse
    {
        [JsonProperty("recipes")]
        public List<Recipe> Recipes { get; set; } = new();

        [JsonProperty("totalCount")]
        public int TotalCount { get; set; }

        [JsonProperty("pageNumber")]
        public int PageNumber { get; set; }

        [JsonProperty("pageSize")]
        public int PageSize { get; set; }

        [JsonProperty("filters")]
        public Dictionary<string, object> Filters { get; set; } = new();
    }
}
