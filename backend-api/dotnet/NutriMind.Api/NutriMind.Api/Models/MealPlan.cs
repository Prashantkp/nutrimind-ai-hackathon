using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace NutriMind.Api.Models
{
    public class MealPlan
    {
        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;

        [JsonProperty("userId")]
        public string UserId { get; set; } = string.Empty;

        [JsonProperty("weekOf")]
        public DateTime WeekOf { get; set; } // Monday of the target week

        [JsonProperty("weekStartDate")]
        public DateTime WeekStartDate => WeekOf; // Alias for WeekOf

        [JsonProperty("weekIdentifier")]
        public string WeekIdentifier { get; set; } = string.Empty; // Format: YYYY-Www (e.g., 2025-W37)

        [JsonProperty("status")]
        public MealPlanStatus Status { get; set; } = MealPlanStatus.Generating;

        [JsonProperty("dailyMeals")]
        public Dictionary<string, DailyMealPlan> DailyMeals { get; set; } = new();

        [JsonProperty("weeklyNutritionSummary")]
        public NutritionSummary WeeklyNutritionSummary { get; set; } = new();

        [JsonProperty("groceryList")]
        public GroceryList GroceryList { get; set; } = new();

        [JsonProperty("totalEstimatedCost")]
        public decimal TotalEstimatedCost { get; set; }

        [JsonProperty("preferences")]
        public MealPlanPreferences Preferences { get; set; } = new();

        [JsonProperty("generationMetadata")]
        public GenerationMetadata GenerationMetadata { get; set; } = new();

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

    public enum MealPlanStatus
    {
        Generating,
        Generated,
        InProgress,
        Completed,
        Cancelled,
        Failed
    }

    public class DailyMealPlan
    {
        [JsonProperty("date")]
        public DateTime Date { get; set; }

        [JsonProperty("dayOfWeek")]
        public string DayOfWeek { get; set; } = string.Empty;

        [JsonProperty("meals")]
        public Dictionary<string, Meal> Meals { get; set; } = new(); // breakfast, lunch, dinner

        [JsonProperty("snacks")]
        public List<Meal> Snacks { get; set; } = new();

        [JsonProperty("dailyNutrition")]
        public NutritionSummary DailyNutrition { get; set; } = new();

        [JsonProperty("dailyEstimatedCost")]
        public decimal DailyEstimatedCost { get; set; }

        [JsonProperty("notes")]
        public string Notes { get; set; } = string.Empty;
    }

    public class Meal
    {
        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;

        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("recipe")]
        public Recipe Recipe { get; set; } = new();

        [JsonProperty("servings")]
        public int Servings { get; set; } = 1;

        [JsonProperty("mealType")]
        public string MealType { get; set; } = string.Empty; // breakfast, lunch, dinner, snack

        [JsonProperty("scheduledTime")]
        public TimeSpan? ScheduledTime { get; set; }

        [JsonProperty("estimatedPrepTime")]
        public int EstimatedPrepTime { get; set; } // minutes

        [JsonProperty("estimatedCookTime")]
        public int EstimatedCookTime { get; set; } // minutes

        [JsonProperty("estimatedCost")]
        public decimal EstimatedCost { get; set; }

        [JsonProperty("nutrition")]
        public NutritionInfo Nutrition { get; set; } = new();

        [JsonProperty("isCompleted")]
        public bool IsCompleted { get; set; } = false;

        [JsonProperty("userRating")]
        public int? UserRating { get; set; } // 1-5 stars

        [JsonProperty("userNotes")]
        public string UserNotes { get; set; } = string.Empty;
    }

    public class Recipe
    {
        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;

        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("cuisine")]
        public string Cuisine { get; set; } = string.Empty;

        [JsonProperty("steps")]
        public List<string> Steps { get; set; } = new();

        [JsonProperty("ingredients")]
        public List<Ingredient> Ingredients { get; set; } = new();

        [JsonProperty("isVegan")]
        public bool IsVegan { get; set; }

        [JsonProperty("isKeto")]
        public bool IsKeto { get; set; }

        [JsonProperty("isDiabeticFriendly")]
        public bool IsDiabeticFriendly { get; set; }

        [JsonProperty("calories")]
        public int Calories { get; set; }

        [JsonProperty("tags")]
        public List<string> Tags { get; set; } = new();

        [JsonProperty("source")]
        public string Source { get; set; } = string.Empty;

        [JsonProperty("difficulty")]
        public string Difficulty { get; set; } = "easy"; // easy, medium, hard

        [JsonProperty("totalTime")]
        public int TotalTime { get; set; } // minutes

        [JsonProperty("servings")]
        public int Servings { get; set; } = 1;
    }

    public class Ingredient
    {
        [JsonProperty("item")]
        public string Item { get; set; } = string.Empty;

        [JsonProperty("qty")]
        public string Qty { get; set; } = string.Empty;

        [JsonProperty("unit")]
        public string Unit { get; set; } = string.Empty;

        [JsonProperty("category")]
        public string Category { get; set; } = string.Empty; // produce, dairy, meat, etc.

        [JsonProperty("estimatedCost")]
        public decimal EstimatedCost { get; set; }

        [JsonProperty("isOptional")]
        public bool IsOptional { get; set; } = false;
    }

    public class NutritionInfo
    {
        [JsonProperty("calories")]
        public int Calories { get; set; }

        [JsonProperty("protein")]
        public decimal Protein { get; set; } // grams

        [JsonProperty("carbohydrates")]
        public decimal Carbohydrates { get; set; } // grams

        [JsonProperty("fats")]
        public decimal Fats { get; set; } // grams

        [JsonProperty("fiber")]
        public decimal Fiber { get; set; } // grams

        [JsonProperty("sugar")]
        public decimal Sugar { get; set; } // grams

        [JsonProperty("sodium")]
        public decimal Sodium { get; set; } // mg
    }

    public class NutritionSummary
    {
        [JsonProperty("totalCalories")]
        public int TotalCalories { get; set; }

        [JsonProperty("totalProtein")]
        public decimal TotalProtein { get; set; }

        [JsonProperty("totalCarbs")]
        public decimal TotalCarbs { get; set; }

        [JsonProperty("totalFats")]
        public decimal TotalFats { get; set; }

        [JsonProperty("averageDailyCalories")]
        public int AverageDailyCalories { get; set; }

        [JsonProperty("targetCalories")]
        public int TargetCalories { get; set; }

        [JsonProperty("adherencePercentage")]
        public decimal AdherencePercentage { get; set; } // how close to targets
    }

    public class GroceryList
    {
        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;

        [JsonProperty("items")]
        public List<GroceryItem> Items { get; set; } = new();

        [JsonProperty("categorizedItems")]
        public Dictionary<string, List<GroceryItem>> CategorizedItems { get; set; } = new();

        [JsonProperty("totalEstimatedCost")]
        public decimal TotalEstimatedCost { get; set; }

        [JsonProperty("generatedAt")]
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

        [JsonProperty("isCompleted")]
        public bool IsCompleted { get; set; } = false;
    }

    public class GroceryItem
    {
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("quantity")]
        public string Quantity { get; set; } = string.Empty;

        [JsonProperty("unit")]
        public string Unit { get; set; } = string.Empty;

        [JsonProperty("category")]
        public string Category { get; set; } = string.Empty;

        [JsonProperty("estimatedCost")]
        public decimal EstimatedCost { get; set; }

        [JsonProperty("isChecked")]
        public bool IsChecked { get; set; } = false;

        [JsonProperty("usedInRecipes")]
        public List<string> UsedInRecipes { get; set; } = new(); // recipe IDs
    }

    public class MealPlanPreferences
    {
        [JsonProperty("dietaryPreference")]
        public string DietaryPreference { get; set; } = string.Empty;

        [JsonProperty("allergens")]
        public List<string> Allergens { get; set; } = new();

        [JsonProperty("dislikes")]
        public List<string> Dislikes { get; set; } = new();

        [JsonProperty("targetCalories")]
        public int TargetCalories { get; set; }

        [JsonProperty("maxPrepTime")]
        public int MaxPrepTime { get; set; } = 30;

        [JsonProperty("budgetConstraint")]
        public decimal BudgetConstraint { get; set; }

        [JsonProperty("varietyLevel")]
        public string VarietyLevel { get; set; } = "medium"; // low, medium, high
    }

    public class GenerationMetadata
    {
        [JsonProperty("startedAt")]
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;

        [JsonProperty("completedAt")]
        public DateTime? CompletedAt { get; set; }

        [JsonProperty("orchestrationId")]
        public string OrchestrationId { get; set; } = string.Empty;

        [JsonProperty("aiModel")]
        public string AiModel { get; set; } = string.Empty;

        [JsonProperty("recipesConsidered")]
        public int RecipesConsidered { get; set; }

        [JsonProperty("generationTime")]
        public TimeSpan GenerationTime { get; set; }

        [JsonProperty("errors")]
        public List<string> Errors { get; set; } = new();
    }
}
