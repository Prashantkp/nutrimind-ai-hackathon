
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace NutriMind.Functions.Models
{
	/// <summary>
	/// Represents a recipe retrieved from Azure AI Search
	/// </summary>
	//public class Recipe
	//{
	//	public string Id { get; set; }
	//	public string Name { get; set; }
	//	public string Cuisine { get; set; }
	//	public List<string> Ingredients { get; set; }
	//	public int Calories { get; set; }
	//	public bool IsVegan { get; set; }
	//	public bool IsKeto { get; set; }
	//	public bool IsDiabeticFriendly { get; set; }
	//	public List<string> Tags { get; set; }
	//}

	/// <summary>
	/// Represents a recipe retrieved from Azure AI Search
	/// </summary>
	public class Recipe
	{
		[JsonPropertyName("id")]
		public string Id { get; set; }

		[JsonPropertyName("name")]
		public string Name { get; set; }

		[JsonPropertyName("cuisine")]
		public string Cuisine { get; set; }

		[JsonPropertyName("steps")]
		public List<string> Steps { get; set; }

		[JsonPropertyName("is_vegan")]
		public bool IsVegan { get; set; }

		[JsonPropertyName("is_keto")]
		public bool IsKeto { get; set; }

		[JsonPropertyName("is_diabetic_friendly")]
		public bool IsDiabeticFriendly { get; set; }

		[JsonPropertyName("calories")]
		public int Calories { get; set; }

		[JsonPropertyName("tags")]
		public List<string> Tags { get; set; }

		[JsonPropertyName("source")]
		public string Source { get; set; }

		[JsonPropertyName("ingredients")]
		public List<Ingredient> Ingredients { get; set; }
	}

	public class Ingredient
	{
		[JsonPropertyName("item")]
		public string Item { get; set; }

		[JsonPropertyName("qty")]
		public string Qty { get; set; }

		[JsonPropertyName("unit")]
		public string Unit { get; set; }
	}
}

