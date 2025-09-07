using System.Collections.Generic;

namespace NutriMind.Functions.Models
{
	public class UserInput
	{
		public string DietaryPreference { get; set; }
		public List<string> Allergens { get; set; }
		public List<string> Dislikes { get; set; }
		public int TargetCalories { get; set; }
	}
}
