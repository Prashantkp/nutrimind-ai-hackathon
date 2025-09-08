using Azure;
using Azure.AI.OpenAI;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using NutriMind.Functions.Models;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel.Primitives;
using System.Text;
using System.Threading.Tasks;

namespace NutriMind.Functions.Services
{
	public class OpenAIService
	{
		private readonly AzureOpenAIClient _client;
		private readonly string _deployment;
		private readonly string _schemaPath;
		private readonly ChatClient _chatClient;
		private JSchema? _mealPlanSchema;

		public OpenAIService(string endpoint, string apiKey, string deployment, string schemaPath)
		{
			_client = new(
				new Uri(endpoint),
					new AzureKeyCredential(apiKey));
			_chatClient = _client.GetChatClient(deployment);
			_deployment = deployment;
			_schemaPath = schemaPath;
		}

		public async Task<string> GenerateMealPlanAsync(UserInput input, List<Recipe> recipes)
		{
			// Build the system prompt with schema and requirements
			var systemPrompt = await BuildSystemPrompt(input);

			// Build the user prompt with recipes and user preferences
			var userPrompt = BuildUserPrompt(input, recipes);

			//_logger.LogInformation($"System prompt length: {systemPrompt.Length}, User prompt length: {userPrompt.Length}");


			List<ChatMessage> messages = new List<ChatMessage>()
			{
				new SystemChatMessage(systemPrompt),
				new UserChatMessage(userPrompt),
			};

			// Support for this recently-launched model with MaxOutputTokenCount parameter requires
			// Azure.AI.OpenAI 2.2.0-beta.4 and SetNewMaxCompletionTokensPropertyEnabled
			//var requestOptions = new ChatCompletionOptions()
			//{
			//	MaxOutputTokenCount = 10000,
			//};

			var response = _chatClient.CompleteChat(messages);
			System.Console.WriteLine(response.Value.Content[0].Text);
			return response.Value.Content[0].Text;
		}

		public bool ValidateMealPlan(string jsonContent, out IList<string> errors)
		{
			var schema = JSchema.Parse(File.ReadAllText(_schemaPath));
			var json = JObject.Parse(jsonContent);
			return json.IsValid(schema, out errors);
		}

		//private string BuildPrompt(UserInput input, List<Recipe> recipes)
		//{
		//	var recipeNames = string.Join(", ", recipes.Select(r => r.Title));
		//	return $"Create a 7-day meal plan for a user with these preferences:\n" +
		//		   $"- Dietary Preference: {input.DietaryPreference}\n" +
		//		   $"- Allergens: {string.Join(", ", input.Allergens)}\n" +
		//		   $"- Disliked Ingredients: {string.Join(", ", input.Dislikes)}\n" +
		//		   $"- Target Calories: {input.TargetCalories}\n\n" +
		//		   $"Available recipes: {recipeNames}\n\n" +
		//		   $"Return only valid JSON that matches the schema.";
		//}

		private async Task LoadSchemaAsync()
		{
			if (_mealPlanSchema != null)
				return;

			try
			{

				// Load schema from embedded resource or file
				//var schemaPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Models", "MealPlanSchema.json");
				var schemaJson = await File.ReadAllTextAsync(_schemaPath);

				_mealPlanSchema = JSchema.Parse(schemaJson);

			}
			catch (Exception ex)
			{
				throw;
			}
		}

		/// <summary>
		/// Builds the system prompt with instructions and schema
		/// </summary>
		private async Task<string> BuildSystemPrompt(UserInput userInput)
		{
			await LoadSchemaAsync();
			var prompt = new StringBuilder();

			prompt.AppendLine("You are an expert nutritionist and meal planner AI assistant. Your task is to create a personalized meal plan based on user preferences and available recipes.");
			prompt.AppendLine();
			prompt.AppendLine("IMPORTANT INSTRUCTIONS:");
			prompt.AppendLine("1. You MUST respond with valid JSON that exactly matches the provided schema");
			prompt.AppendLine("2. Use only the recipes provided in the user's message");
			prompt.AppendLine("3. Ensure the total daily calories are close to the user's target");
			prompt.AppendLine("4. Respect all dietary preferences, allergens, and dislikes");
			prompt.AppendLine("5. Balance macronutrients appropriately");
			prompt.AppendLine("6. Include variety across meals and days");
			prompt.AppendLine("7. Provide realistic serving sizes");
			prompt.AppendLine();
			prompt.AppendLine("JSON SCHEMA TO FOLLOW:");
			prompt.AppendLine("Your response must be a valid JSON object matching this exact schema:");
			prompt.AppendLine();

			// Include the schema in the prompt
			if (_mealPlanSchema != null)
			{
				prompt.AppendLine(_mealPlanSchema.ToString());
			}

			prompt.AppendLine();
			prompt.AppendLine("RESPONSE FORMAT:");
			prompt.AppendLine("Respond ONLY with the JSON object. Do not include any explanatory text before or after the JSON.");

			return prompt.ToString();
		}

		/// <summary>
		/// Builds the user prompt with preferences and available recipes
		/// </summary>
		private string BuildUserPrompt(UserInput userInput, List<Recipe> recipes)
		{
			var prompt = new StringBuilder();

			prompt.AppendLine("Please create a meal plan with the following requirements:");
			prompt.AppendLine();
			prompt.AppendLine("USER PREFERENCES:");
			prompt.AppendLine($"- Target calories per day: {userInput.TargetCalories}");
			prompt.AppendLine($"- Dietary preference: {userInput.DietaryPreference}");
			prompt.AppendLine($"- Number of meals per day: {3}");
			prompt.AppendLine($"- Number of days to plan: {7}");

			if (userInput.Allergens?.Any() == true)
			{
				prompt.AppendLine($"- Allergens to avoid: {string.Join(", ", userInput.Allergens)}");
			}

			if (userInput.Dislikes?.Any() == true)
			{
				prompt.AppendLine($"- Dislikes to avoid: {string.Join(", ", userInput.Dislikes)}");
			}

			prompt.AppendLine();
			prompt.AppendLine("AVAILABLE RECIPES:");

			// Group recipes by meal type for better organization
			//var recipesByMealType = recipes.GroupBy(r => r.MealType).ToList();
			//var recipesByMealType = recipes.ToList();
			//foreach (var mealGroup in recipesByMealType)
			//{
			//	prompt.AppendLine($"\n{mealGroup.Key.ToUpper()} RECIPES:");

			//	foreach (var recipe in mealGroup.Take(15)) // Limit to prevent token overflow
			//	{
			//		prompt.AppendLine($"\nRecipe ID: {recipe.Id}");
			//		prompt.AppendLine($"Title: {recipe.Title}");
			//		prompt.AppendLine($"Description: {recipe.Description}");
			//		prompt.AppendLine($"Calories per serving: {recipe.CaloriesPerServing}");
			//		prompt.AppendLine($"Servings: {recipe.Servings}");
			//		prompt.AppendLine($"Prep time: {recipe.PrepTimeMinutes} min");
			//		prompt.AppendLine($"Cook time: {recipe.CookTimeMinutes} min");
			//		prompt.AppendLine($"Difficulty: {recipe.Difficulty}");
			//		prompt.AppendLine($"Cuisine: {recipe.Cuisine}");

			//		if (recipe.DietaryTags?.Any() == true)
			//		{
			//			prompt.AppendLine($"Dietary tags: {string.Join(", ", recipe.DietaryTags)}");
			//		}

			//		if (recipe.Ingredients?.Any() == true)
			//		{
			//			prompt.AppendLine($"Key ingredients: {string.Join(", ", recipe.Ingredients.Take(5))}");
			//		}

			//		prompt.AppendLine($"Nutrition - Protein: {recipe.Nutrition.Protein}g, Carbs: {recipe.Nutrition.Carbohydrates}g, Fat: {recipe.Nutrition.Fat}g");
			//	}
			//}

			prompt.AppendLine();
			prompt.AppendLine("Create a balanced meal plan using these recipes. Ensure variety, nutritional balance, and adherence to all user preferences.");

			return prompt.ToString();
		}

	}
}