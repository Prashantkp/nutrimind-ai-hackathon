using System.Collections.Generic;

namespace NutriMind.Functions.Models
{
    /// <summary>
    /// Represents the input data from the user for meal plan generation
    /// </summary>
    public class UserInput
    {
        /// <summary>
        /// Dietary preference (e.g., "vegetarian", "vegan", "keto", "mediterranean", etc.)
        /// </summary>
        public string DietaryPreference { get; set; } = string.Empty;

        /// <summary>
        /// List of allergens to avoid (e.g., "nuts", "dairy", "gluten", etc.)
        /// </summary>
        public List<string> Allergens { get; set; } = new List<string>();

        /// <summary>
        /// List of foods or ingredients the user dislikes
        /// </summary>
        public List<string> Dislikes { get; set; } = new List<string>();

        /// <summary>
        /// Target daily calorie intake
        /// </summary>
        public int TargetCalories { get; set; }

      
      
        /// <summary>
        /// Number of meals per day (optional, defaults to 3)
        /// </summary>
        public int MealsPerDay { get; set; } = 3;

        /// <summary>
        /// Number of days to plan for (optional, defaults to 1)
        /// </summary>
        public int PlanDays { get; set; } = 1;

        /// <summary>
        /// Additional dietary restrictions or notes
        /// </summary>
        public string AdditionalNotes { get; set; } = string.Empty;
    }
}
