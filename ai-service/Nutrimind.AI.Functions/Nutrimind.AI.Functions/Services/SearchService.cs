using Azure;
using Azure.Identity;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Logging;
using NutriMind.Functions.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NutriMind.Functions.Services
{
	/// <summary>
	/// Service for querying recipes from Azure AI Search
	/// </summary>
	public class SearchService
	{
		private readonly SearchClient _searchClient;

		public SearchService(string endpoint, string apiKey)
		{
			_searchClient = new SearchClient(new Uri(endpoint), "azureblob-index", new AzureKeyCredential(apiKey));
		}

		public async Task<List<Recipe>> QueryRecipesAsync(UserInput input)
		{
			try
			{
				var options = new SearchOptions
				{
					Size = 10,
					Filter = BuildFilter(input)
				};
				var results = await _searchClient.SearchAsync<Recipe>("*", options);

				var recipes = new List<Recipe>();
				await foreach (var result in results.Value.GetResultsAsync())
				{
					recipes.Add(result.Document);

				}
				return recipes;
			}
			catch (RequestFailedException ex)
			{
				// Handle Azure Search specific exceptions
				throw new InvalidOperationException($"Search request failed: {ex.Message}", ex);
			}
			catch (Exception ex)
			{
				// Handle any other unexpected exceptions
				throw new InvalidOperationException($"An error occurred while querying recipes: {ex.Message}", ex);
			}
		}

		private string BuildFilter(UserInput input)
		{
			var filters = new List<string>();

			if (input.DietaryPreference == "vegan")
				filters.Add("is_vegan eq true");
			if (input.DietaryPreference == "keto")
				filters.Add("is_keto eq true");
			if (input.DietaryPreference == "diabetic")
				filters.Add("is_diabetic_friendly eq true");
			if (input.TargetCalories > 0)
				filters.Add($"calories ge {input.TargetCalories - 100} and calories le {input.TargetCalories + 100}");

			// Since ingredients is now a complex object with 'item' property
			if (input.Allergens != null)
			{
				foreach (var allergen in input.Allergens)
				{
					var escapedAllergen = allergen.Replace("'", "''");
					filters.Add($"not ingredients/any(ingredient: ingredient/item eq '{escapedAllergen}')");
				}
			}

			if (input.Dislikes != null)
			{
				foreach (var dislike in input.Dislikes)
				{
					var escapedDislike = dislike.Replace("'", "''");
					filters.Add($"not ingredients/any(ingredient: ingredient/item eq '{escapedDislike}')");
				}
			}

			return filters.Any() ? string.Join(" and ", filters) : "";
		}
	}
}
