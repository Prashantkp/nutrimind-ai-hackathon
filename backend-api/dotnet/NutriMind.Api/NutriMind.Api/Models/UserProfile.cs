using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace NutriMind.Api.Models
{
    public class UserProfile
    {
        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;

        [JsonProperty("userId")]
        public string UserId { get; set; } = string.Empty;

        [JsonProperty("email")]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [JsonProperty("phoneNumber")]
        public string? PhoneNumber { get; set; }

        [JsonProperty("firstName")]
        public string FirstName { get; set; } = string.Empty;

        [JsonProperty("lastName")]
        public string LastName { get; set; } = string.Empty;

        [JsonProperty("age")]
        [Range(1, 150)]
        public int Age { get; set; }

        [JsonProperty("height")]
        public float Height { get; set; } // in cm

        [JsonProperty("weight")]
        public float Weight { get; set; } // in kg

        [JsonProperty("activityLevel")]
        public string ActivityLevel { get; set; } = string.Empty; // sedentary, light, moderate, active, very_active

        [JsonProperty("dietaryPreference")]
        public string DietaryPreference { get; set; } = string.Empty; // vegan, vegetarian, keto, paleo, mediterranean, etc.

        [JsonProperty("allergens")]
        public List<string> Allergens { get; set; } = new();

        [JsonProperty("dislikes")]
        public List<string> Dislikes { get; set; } = new();

        [JsonProperty("healthGoals")]
        public List<string> HealthGoals { get; set; } = new(); // weight_loss, muscle_gain, maintain_weight, etc.

        [JsonProperty("targetCalories")]
        public int TargetCalories { get; set; }

        [JsonProperty("targetProtein")]
        public int TargetProtein { get; set; } // in grams

        [JsonProperty("targetCarbs")]
        public int TargetCarbs { get; set; } // in grams

        [JsonProperty("targetFats")]
        public int TargetFats { get; set; } // in grams

        [JsonProperty("mealFrequency")]
        public int MealFrequency { get; set; } = 3; // meals per day

        [JsonProperty("snackFrequency")]
        public int SnackFrequency { get; set; } = 2; // snacks per day

        [JsonProperty("cookingSkillLevel")]
        public string CookingSkillLevel { get; set; } = "beginner"; // beginner, intermediate, advanced

        [JsonProperty("cookingTimePreference")]
        public int CookingTimePreference { get; set; } = 30; // max minutes per meal

        [JsonProperty("budgetPerWeek")]
        public decimal BudgetPerWeek { get; set; }

        [JsonProperty("preferredCuisines")]
        public List<string> PreferredCuisines { get; set; } = new();

        [JsonProperty("kitchenEquipment")]
        public List<string> KitchenEquipment { get; set; } = new();

        [JsonProperty("shoppingPreference")]
        public string ShoppingPreference { get; set; } = "grocery_store"; // grocery_store, online_delivery, farmers_market

        [JsonProperty("notificationPreferences")]
        public NotificationPreferences NotificationPreferences { get; set; } = new();

        [JsonProperty("preferences")]
        public UserPreferences? Preferences { get; set; }

        [JsonProperty("connectedServices")]
        public ConnectedServices ConnectedServices { get; set; } = new();

        [JsonProperty("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [JsonProperty("updatedAt")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [JsonProperty("isActive")]
        public bool IsActive { get; set; } = true;

        // Partition key for Cosmos DB
        [JsonProperty("partitionKey")]
        public string PartitionKey => UserId;
    }

    public class NotificationPreferences
    {
        [JsonProperty("emailEnabled")]
        public bool EmailEnabled { get; set; } = true;

        [JsonProperty("pushEnabled")]
        public bool PushEnabled { get; set; } = true;

        [JsonProperty("mealReminders")]
        public bool MealReminders { get; set; } = true;

        [JsonProperty("groceryReminders")]
        public bool GroceryReminders { get; set; } = true;

        [JsonProperty("weeklyPlanReminders")]
        public bool WeeklyPlanReminders { get; set; } = true;

        [JsonProperty("nutritionTips")]
        public bool NutritionTips { get; set; } = false;

        [JsonProperty("reminderTimes")]
        public List<string> ReminderTimes { get; set; } = new List<string> { "09:00", "12:00", "18:00" };

        [JsonProperty("shoppingReminders")]
        public bool ShoppingReminders { get; set; } = true;

        [JsonProperty("weeklyPlanGeneration")]
        public bool WeeklyPlanGeneration { get; set; } = true;

        [JsonProperty("nutritionInsights")]
        public bool NutritionInsights { get; set; } = true;

        [JsonProperty("preferredReminderTime")]
        public TimeSpan PreferredReminderTime { get; set; } = new(10, 0, 0); // 10 AM default
    }

    public class ConnectedServices
    {
        [JsonProperty("instacart")]
        public ServiceConnection? Instacart { get; set; }

        [JsonProperty("amazon_fresh")]
        public ServiceConnection? AmazonFresh { get; set; }

        [JsonProperty("walmart_grocery")]
        public ServiceConnection? WalmartGrocery { get; set; }

        [JsonProperty("kroger")]
        public ServiceConnection? Kroger { get; set; }
    }

    public class ServiceConnection
    {
        [JsonProperty("isConnected")]
        public bool IsConnected { get; set; } = false;

        [JsonProperty("accessToken")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonProperty("refreshToken")]
        public string RefreshToken { get; set; } = string.Empty;

        [JsonProperty("expiresAt")]
        public DateTime? ExpiresAt { get; set; }

        [JsonProperty("connectedAt")]
        public DateTime? ConnectedAt { get; set; }
    }

    public class UserPreferences
    {
        [JsonProperty("notifications")]
        public NotificationPreferences? Notifications { get; set; }
    }
}
