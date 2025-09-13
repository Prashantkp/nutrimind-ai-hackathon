using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using NutriMind.Api.Models;
using OpenAI.Chat;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NutriMind.Api.Services
{
    public interface IOpenAIService
    {
        Task<string> GenerateWeeklyMealPlanAsync(UserProfile userProfile, List<Recipe> candidateRecipes);
        Task<MealPlan> ParseMealPlanResponseAsync(string aiResponse, string userId, string weekIdentifier);
        Task<string> ValidateNutritionAsync(MealPlan mealPlan, UserProfile userProfile);
        Task<List<string>> GenerateHealthInsightsAsync(UserProfile userProfile, MealPlan mealPlan);
    }

    public class OpenAIService : IOpenAIService
    {
        private readonly AzureOpenAIClient _openAIClient;
        private readonly ILogger<OpenAIService> _logger;
        private readonly string _deploymentName;
		private readonly ChatClient _chatClient;
		private JSchema? _mealPlanSchema;
        private readonly string _schemaPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Models", "MealPlanSchema.json");

		public OpenAIService(IConfiguration configuration, ILogger<OpenAIService> logger)
        {
            _logger = logger;
			var keyVaultUri = configuration["KeyVaultUri"];
			var kvClient = new SecretClient(new Uri(keyVaultUri), new DefaultAzureCredential());
			var endpoint = kvClient.GetSecret("openai-endpoint").Value.Value; 
            var apiKey = kvClient.GetSecret("openai-api-key").Value.Value;
			_deploymentName = configuration["OpenAiDeployment"] ?? "gpt-5-mini";

			if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(apiKey))
            {
                _logger.LogWarning("Azure OpenAI configuration is missing. AI functionality will use mock responses.");
                _openAIClient = null!;
                return;
            }

            _openAIClient = new AzureOpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
			_chatClient = _openAIClient.GetChatClient(_deploymentName);
		}

        public async Task<string> GenerateWeeklyMealPlanAsync(UserProfile userProfile, List<Recipe> candidateRecipes)
        {
            _logger.LogInformation("Generating weekly meal plan for user {UserId}", userProfile.UserId);

			// Return mock response to avoid OpenAI client issues during development
			//return GenerateMockMealPlan(userProfile, candidateRecipes);

			// Build the system prompt with schema and requirements
			var systemPrompt = await BuildSystemPrompt(userProfile);

			// Build the user prompt with recipes and user preferences
			var userPrompt = BuildUserPrompt(userProfile, candidateRecipes);

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
		private async Task<string> BuildSystemPrompt(UserProfile userInput)
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
        private string BuildUserPrompt(UserProfile userInput, List<Recipe> recipes)
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

			//Group recipes by meal type for better organization

			// var recipesByMealType = recipes.GroupBy(r => r.MealType).ToList();
			var recipesByMealType = recipes.ToList();
			foreach (var recipe in recipesByMealType)
			{
				prompt.AppendLine($"\n RECIPES:");

				//foreach (var recipe in mealGroup.Take(15)) // Limit to prevent token overflow
				//{
				prompt.AppendLine($"\nRecipe ID: {recipe.Id}");
				prompt.AppendLine($"Name: {recipe.Name}");
				prompt.AppendLine($"Calories: {recipe.Calories}");
				prompt.AppendLine($"Servings: {recipe.Servings}");
				prompt.AppendLine($"Total time: {recipe.TotalTime} min");
				prompt.AppendLine($"Difficulty: {recipe.Difficulty}");
				prompt.AppendLine($"Cuisine: {recipe.Cuisine}");

				if (recipe.Tags?.Any() == true)
				{
					prompt.AppendLine($"Tags: {string.Join(", ", recipe.Tags)}");
				}

				if (recipe.Ingredients?.Any() == true)
				{
					prompt.AppendLine($"Key ingredients: {string.Join(", ", recipe.Ingredients.Take(5).Select(i => i.Item ?? i.ToString()))}");
				}

				prompt.AppendLine($"Source: {recipe.Source}");
				//}
			}

			prompt.AppendLine();
            prompt.AppendLine("Create a balanced meal plan using these recipes. Ensure variety, nutritional balance, and adherence to all user preferences.");

            return prompt.ToString();
        }


        public async Task<MealPlan> ParseMealPlanResponseAsync(string aiResponse, string userId, string weekIdentifier)
        {
            try
            {
                // Clean up the response to extract JSON
                var jsonStart = aiResponse.IndexOf('{');
                var jsonEnd = aiResponse.LastIndexOf('}');
                
                if (jsonStart >= 0 && jsonEnd >= 0 && jsonEnd > jsonStart)
                {
                    var jsonContent = aiResponse[jsonStart..(jsonEnd + 1)];
                    var aiMealPlan = JsonSerializer.Deserialize<AIMealPlanResponse>(jsonContent);
                    
                    return await ConvertAIResponseToMealPlan(aiMealPlan!, userId, weekIdentifier);
                }
                else
                {
                    _logger.LogWarning("Could not extract JSON from AI response, using fallback parsing");
                    return CreateFallbackMealPlan(userId, weekIdentifier);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing meal plan response");
                return CreateFallbackMealPlan(userId, weekIdentifier);
            }
        }

        public async Task<string> ValidateNutritionAsync(MealPlan mealPlan, UserProfile userProfile)
        {
            _logger.LogInformation("Validating nutrition for meal plan {MealPlanId}", mealPlan.Id);
            
            // Return mock validation response for development
            return "Nutrition validation completed. Plan appears to meet general dietary guidelines and aligns well with your target calorie and macronutrient goals.";
        }

        public async Task<List<string>> GenerateHealthInsightsAsync(UserProfile userProfile, MealPlan mealPlan)
        {
            // This would generate personalized health insights based on the user's profile and meal plan
            // For now, returning static insights
            return new List<string>
            {
                "Your meal plan provides excellent variety with recipes from different cuisines.",
                "Consider adding more leafy greens to increase micronutrient density.",
                "Your protein intake is well-distributed throughout the day for optimal muscle synthesis.",
                "The plan includes healthy fats from sources like avocado and nuts."
            };
        }

   //     private string BuildUserPrompt(UserProfile userProfile, List<Recipe> candidateRecipes)
   //     {
   //         var sb = new StringBuilder();
			//sb.AppendLine("Please create a meal plan with the following requirements:");
			//sb.AppendLine($"User Profile:");
   //         sb.AppendLine($"- Age: {userProfile.Age}, Weight: {userProfile.Weight}kg, Height: {userProfile.Height}cm");
   //         sb.AppendLine($"- Activity Level: {userProfile.ActivityLevel}");
   //         sb.AppendLine($"- Dietary Preference: {userProfile.DietaryPreference}");
   //         sb.AppendLine($"- Allergens: {string.Join(", ", userProfile.Allergens)}");
   //         sb.AppendLine($"- Dislikes: {string.Join(", ", userProfile.Dislikes)}");
   //         sb.AppendLine($"- Target Calories: {userProfile.TargetCalories} per day");
   //         sb.AppendLine($"- Cooking Skill: {userProfile.CookingSkillLevel}");
   //         sb.AppendLine($"- Max Cooking Time: {userProfile.CookingTimePreference} minutes");
   //         sb.AppendLine($"- Weekly Budget: ${userProfile.BudgetPerWeek}");
   //         sb.AppendLine();

   //         sb.AppendLine($"Available Recipes ({candidateRecipes.Count} total):");
   //         foreach (var recipe in candidateRecipes.Take(20)) // Limit to prevent prompt overflow
   //         {
   //             sb.AppendLine($"- ID: {recipe.Id}, Name: {recipe.Name}, Calories: {recipe.Calories}, " +
   //                          $"Vegan: {recipe.IsVegan}, Keto: {recipe.IsKeto}, Time: {recipe.TotalTime}min");
   //         }

   //         return sb.ToString();
   //     }

        private string GenerateMockMealPlan()
        {
            return JsonSerializer.Serialize(new AIMealPlanResponse
            {
                WeeklyPlan = new Dictionary<string, DayPlan>
                {
                    ["monday"] = new DayPlan
                    {
                        Breakfast = new MealReference { RecipeId = "recipe-1", Servings = 1 },
                        Lunch = new MealReference { RecipeId = "recipe-2", Servings = 1 },
                        Dinner = new MealReference { RecipeId = "recipe-1", Servings = 1 },
                        Snacks = new List<MealReference>
                        {
                            new() { RecipeId = "recipe-2", Servings = 1 }
                        }
                    }
                },
                NutritionSummary = new AINetritionSummary
                {
                    TotalCalories = 2000,
                    AvgDailyCalories = 2000,
                    Protein = 150,
                    Carbs = 200,
                    Fats = 80
                }
            });
        }

        private async Task<MealPlan> ConvertAIResponseToMealPlan(AIMealPlanResponse aiResponse, string userId, string weekIdentifier)
        {
            var mealPlan = new MealPlan
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId,
                WeekIdentifier = weekIdentifier,
                WeekOf = Helpers.WeekHelper.GetMondayOfWeek(weekIdentifier),
                Status = MealPlanStatus.Generated
            };

            // Convert daily plans (this is simplified - would need recipe lookup in production)
            foreach (var kvp in aiResponse.WeeklyPlan)
            {
                var dayPlan = new DailyMealPlan
                {
                    Date = mealPlan.WeekOf.AddDays(GetDayOffset(kvp.Key)),
                    DayOfWeek = kvp.Key
                };

                // This would typically involve looking up recipes and calculating nutrition
                // For demo purposes, using placeholder data
                dayPlan.Meals["breakfast"] = CreateMealFromReference(kvp.Value.Breakfast, "breakfast");
                dayPlan.Meals["lunch"] = CreateMealFromReference(kvp.Value.Lunch, "lunch");
                dayPlan.Meals["dinner"] = CreateMealFromReference(kvp.Value.Dinner, "dinner");

                mealPlan.DailyMeals[kvp.Key] = dayPlan;
            }

            return mealPlan;
        }

        private Meal CreateMealFromReference(MealReference mealRef, string mealType)
        {
            // In production, this would lookup the actual recipe
            return new Meal
            {
                Id = Guid.NewGuid().ToString(),
                Name = $"Mock {mealType}",
                Recipe = new Recipe { Id = mealRef.RecipeId, Name = $"Mock Recipe {mealRef.RecipeId}" },
                Servings = mealRef.Servings,
                MealType = mealType,
                EstimatedCost = 5.00m,
                Nutrition = new NutritionInfo { Calories = 400 }
            };
        }

        private MealPlan CreateFallbackMealPlan(string userId, string weekIdentifier)
        {
            return new MealPlan
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId,
                WeekIdentifier = weekIdentifier,
                WeekOf = Helpers.WeekHelper.GetMondayOfWeek(weekIdentifier),
                Status = MealPlanStatus.Generated
            };
        }

        private int GetDayOffset(string dayName)
        {
            return dayName.ToLower() switch
            {
                "monday" => 0,
                "tuesday" => 1,
                "wednesday" => 2,
                "thursday" => 3,
                "friday" => 4,
                "saturday" => 5,
                "sunday" => 6,
                _ => 0
            };
        }

        // Internal classes for AI response parsing
        private class AIMealPlanResponse
        {
            [JsonPropertyName("weekly_plan")]
            public Dictionary<string, DayPlan> WeeklyPlan { get; set; } = new();

            [JsonPropertyName("nutrition_summary")]
            public AINetritionSummary NutritionSummary { get; set; } = new();
        }

        private class DayPlan
        {
            [JsonPropertyName("breakfast")]
            public MealReference Breakfast { get; set; } = new();

            [JsonPropertyName("lunch")]
            public MealReference Lunch { get; set; } = new();

            [JsonPropertyName("dinner")]
            public MealReference Dinner { get; set; } = new();

            [JsonPropertyName("snacks")]
            public List<MealReference> Snacks { get; set; } = new();
        }

        private class MealReference
        {
            [JsonPropertyName("recipe_id")]
            public string RecipeId { get; set; } = string.Empty;

            [JsonPropertyName("servings")]
            public int Servings { get; set; } = 1;
        }

        private class AINetritionSummary
        {
            [JsonPropertyName("total_calories")]
            public int TotalCalories { get; set; }

            [JsonPropertyName("avg_daily_calories")]
            public int AvgDailyCalories { get; set; }

            [JsonPropertyName("protein")]
            public decimal Protein { get; set; }

            [JsonPropertyName("carbs")]
            public decimal Carbs { get; set; }

            [JsonPropertyName("fats")]
            public decimal Fats { get; set; }
        }

        private string GenerateMockMealPlan(UserProfile userProfile, List<Recipe> candidateRecipes)
        {
            // Generate a simple mock meal plan for development purposes
            var mockPlan = @"{
                ""weekly_plan"": {
                    ""monday"": {
                        ""breakfast"": { ""recipe_id"": ""recipe-1"", ""servings"": 1 },
                        ""lunch"": { ""recipe_id"": ""recipe-2"", ""servings"": 1 },
                        ""dinner"": { ""recipe_id"": ""recipe-3"", ""servings"": 1 },
                        ""snacks"": [
                            { ""recipe_id"": ""recipe-4"", ""servings"": 1 }
                        ]
                    },
                    ""tuesday"": {
                        ""breakfast"": { ""recipe_id"": ""recipe-5"", ""servings"": 1 },
                        ""lunch"": { ""recipe_id"": ""recipe-6"", ""servings"": 1 },
                        ""dinner"": { ""recipe_id"": ""recipe-7"", ""servings"": 1 },
                        ""snacks"": [
                            { ""recipe_id"": ""recipe-8"", ""servings"": 1 }
                        ]
                    },
                    ""wednesday"": {
                        ""breakfast"": { ""recipe_id"": ""recipe-1"", ""servings"": 1 },
                        ""lunch"": { ""recipe_id"": ""recipe-2"", ""servings"": 1 },
                        ""dinner"": { ""recipe_id"": ""recipe-3"", ""servings"": 1 },
                        ""snacks"": [
                            { ""recipe_id"": ""recipe-4"", ""servings"": 1 }
                        ]
                    },
                    ""thursday"": {
                        ""breakfast"": { ""recipe_id"": ""recipe-5"", ""servings"": 1 },
                        ""lunch"": { ""recipe_id"": ""recipe-6"", ""servings"": 1 },
                        ""dinner"": { ""recipe_id"": ""recipe-7"", ""servings"": 1 },
                        ""snacks"": [
                            { ""recipe_id"": ""recipe-8"", ""servings"": 1 }
                        ]
                    },
                    ""friday"": {
                        ""breakfast"": { ""recipe_id"": ""recipe-1"", ""servings"": 1 },
                        ""lunch"": { ""recipe_id"": ""recipe-2"", ""servings"": 1 },
                        ""dinner"": { ""recipe_id"": ""recipe-3"", ""servings"": 1 },
                        ""snacks"": [
                            { ""recipe_id"": ""recipe-4"", ""servings"": 1 }
                        ]
                    },
                    ""saturday"": {
                        ""breakfast"": { ""recipe_id"": ""recipe-5"", ""servings"": 1 },
                        ""lunch"": { ""recipe_id"": ""recipe-6"", ""servings"": 1 },
                        ""dinner"": { ""recipe_id"": ""recipe-7"", ""servings"": 1 },
                        ""snacks"": [
                            { ""recipe_id"": ""recipe-8"", ""servings"": 1 }
                        ]
                    },
                    ""sunday"": {
                        ""breakfast"": { ""recipe_id"": ""recipe-1"", ""servings"": 1 },
                        ""lunch"": { ""recipe_id"": ""recipe-2"", ""servings"": 1 },
                        ""dinner"": { ""recipe_id"": ""recipe-3"", ""servings"": 1 },
                        ""snacks"": [
                            { ""recipe_id"": ""recipe-4"", ""servings"": 1 }
                        ]
                    }
                },
                ""nutrition_summary"": {
                    ""total_calories"": " + (userProfile.TargetCalories * 7) + @",
                    ""avg_daily_calories"": " + userProfile.TargetCalories + @",
                    ""protein"": " + (userProfile.TargetProtein * 7) + @",
                    ""carbs"": " + (userProfile.TargetCarbs * 7) + @",
                    ""fats"": " + (userProfile.TargetFats * 7) + @"
                }
            }";
            
            return mockPlan;
        }
    }
}
