using Azure;
using Azure.AI.OpenAI;
using Azure.AI.OpenAI.Chat;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
        Task<string> ProvideHealthyTip(string userPrompt);
    }

    public class OpenAIService : IOpenAIService
    {
        private readonly AzureOpenAIClient _openAIClient;
        private readonly ILogger<OpenAIService> _logger;
        private readonly string _deploymentName;
		private readonly ChatClient _chatClient;
		private JSchema? _mealPlanSchema;
        private readonly string _schemaPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Models", "MealPlanSchema.json");

        private const string ChatAssistantInstruction = @"
            You are an AI assistant that helps people with healthy tips for maintaining good food habits. 
            Provide them motivation with proven scientific facts about advantage of healthy eating habits. Keep the response limited to 200 characters. 
            Be creative and funny sometime while providing a quote.Suggest intuitive font clor and background color based on the quote. 
            Provide the response in the JSON format with tip,fontcolor and backgroundcolor properties.";

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

            // Configure client with timeout
            var clientOptions = new AzureOpenAIClientOptions()
            {
                NetworkTimeout = TimeSpan.FromMinutes(5) // 5-minute timeout
            };

            _openAIClient = new AzureOpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey), clientOptions);
			_chatClient = _openAIClient.GetChatClient(_deploymentName);
		}

        public async Task<string> ProvideHealthyTip(string userPrompt)
        {
            if (_openAIClient == null)
            {
                // Mock response when OpenAI client is not configured
                return "Eat more veggies! Healthy eating is the key to a happy life!";
            }
            try
            {
                var messages = new List<ChatMessage>
                {
                    new SystemChatMessage(ChatAssistantInstruction),
                    new UserChatMessage(userPrompt)
                };
                ChatCompletion completion = await _chatClient.CompleteChatAsync(messages);
                string response = completion.Content[0].Text.Trim();
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while calling Azure OpenAI service.");
                return "Sorry, I couldn't fetch a healthy tip at the moment. Please try again later.";
            }
        }
        public async Task<string> GenerateWeeklyMealPlanAsync(UserProfile userProfile, List<Recipe> candidateRecipes)
        {
            _logger.LogInformation("Generating weekly meal plan for user {UserId}", userProfile.UserId);

			// For fastest performance during development/testing
			if (userProfile.TargetCalories < 100) // Use as a flag for fast mode
			{
				return GenerateMockMealPlan(userProfile, candidateRecipes);
			}

			// Build the optimized system prompt
			var systemPrompt = BuildOptimizedSystemPrompt();

			// Build the simplified user prompt
			var userPrompt = BuildOptimizedUserPrompt(userProfile, candidateRecipes);

			_logger.LogInformation($"System prompt length: {systemPrompt.Length}, User prompt length: {userPrompt.Length}");

			List<ChatMessage> messages = new List<ChatMessage>()
			{
				new SystemChatMessage(systemPrompt),
				new UserChatMessage(userPrompt),
			};

			// Configure chat completion options for better performance
			//var requestOptions = new ChatCompletionOptions()
			//{
			//	//Temperature = 0.1f, // Lower temperature for more consistent JSON output
			//	TopP = 0.8f,
			//	FrequencyPenalty = 0.0f,
			//	PresencePenalty = 0.0f,
			//	// Increased token limit for full 7-day meal plan
			//	//MaxOutputTokenCount = 6000,  // Increased to accommodate full 7-day plan
			//};
#pragma warning disable AOAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
			//requestOptions.SetNewMaxCompletionTokensPropertyEnabled();
#pragma warning restore AOAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

			// Add this using directive at the top of the file with the other OpenAI/Azure imports

			try
			{
				// Add cancellation token for timeout control
				using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(3));
				
				// Use async method with timeout handling
				var response = await _chatClient.CompleteChatAsync(messages, null, cts.Token);
				var result = response.Value.Content[0].Text;
				_logger.LogInformation("Successfully generated meal plan response, length: {Length}", result.Length);
				return result;
			}
			catch (OperationCanceledException)
			{
				_logger.LogWarning("OpenAI request timed out after 3 minutes, returning mock data");
				return GenerateMockMealPlan(userProfile, candidateRecipes);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error generating meal plan with OpenAI, returning mock data");
				return GenerateMockMealPlan(userProfile, candidateRecipes);
			}
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
		/// Builds an optimized system prompt without the full schema
		/// </summary>
		private string BuildOptimizedSystemPrompt()
		{
			return @"Generate a COMPLETE 7-day meal plan as valid JSON only. No explanations.

CRITICAL: You MUST include ALL 7 days: monday, tuesday, wednesday, thursday, friday, saturday, sunday.

Structure example (EXPAND to all 7 days):
{
  ""id"": ""plan-1"",
  ""userId"": ""user"",
  ""weekIdentifier"": ""2025-W37"",
  ""dailyMeals"": {
    ""monday"": {
      ""date"": ""2025-09-08T00:00:00Z"",
      ""dayOfWeek"": ""monday"",
      ""meals"": {
        ""breakfast"": {
          ""id"": ""meal-mon-b"",
          ""name"": ""Oatmeal"",
          ""recipe"": {
            ""id"": ""rec-1"",
            ""name"": ""Oatmeal"",
            ""cuisine"": ""American"",
            ""steps"": [""Boil water"", ""Add oats"", ""Cook 5 min""],
            ""ingredients"": [{""item"": ""oats"", ""qty"": ""1"", ""unit"": ""cup"", ""category"": ""grains"", ""estimatedCost"": 1.5}],
            ""calories"": 300,
            ""difficulty"": ""easy"",
            ""totalTime"": 10,
            ""servings"": 1
          },
          ""servings"": 1,
          ""mealType"": ""breakfast"",
          ""nutrition"": {""calories"": 300, ""protein"": 8.0, ""carbohydrates"": 54.0, ""fats"": 6.0}
        },
        ""lunch"": { /*similar structure*/ },
        ""dinner"": { /*similar structure*/ }
      }
    },
    ""tuesday"": { /*same structure as monday but different meals*/ },
    ""wednesday"": { /*same structure as monday but different meals*/ },
    ""thursday"": { /*same structure as monday but different meals*/ },
    ""friday"": { /*same structure as monday but different meals*/ },
    ""saturday"": { /*same structure as monday but different meals*/ },
    ""sunday"": { /*same structure as monday but different meals*/ }
  }
}

MANDATORY REQUIREMENTS:
- Generate ALL 7 days of the week (monday through sunday)
- Each day must have 3 meals: breakfast, lunch, dinner
- Use unique recipes for variety
- Include realistic nutrition values
- Add simple cooking steps
- Use common ingredients with USD costs
- DO NOT use placeholder text like '...' or 'similar structure'
- Generate the complete JSON structure";
		}

        /// <summary>
        /// Builds an optimized user prompt with essential requirements only
        /// </summary>
        private string BuildOptimizedUserPrompt(UserProfile userInput, List<Recipe> recipes)
        {
            var prompt = new StringBuilder();

            prompt.AppendLine("Create a COMPLETE 7-day meal plan (Monday through Sunday) with:");
            prompt.AppendLine($"• Target calories: {(userInput.TargetCalories == 0 ? 2000 : userInput.TargetCalories)}/day");
            prompt.AppendLine($"• Diet: {userInput.DietaryPreference ?? "None"}");

            if (userInput.Allergens?.Any() == true)
            {
                prompt.AppendLine($"• Avoid allergens: {string.Join(", ", userInput.Allergens)}");
            }

            if (userInput.Dislikes?.Any() == true)
            {
                prompt.AppendLine($"• Avoid dislikes: {string.Join(", ", userInput.Dislikes)}");
            }

            prompt.AppendLine();
            prompt.AppendLine("MANDATORY REQUIREMENTS:");
            prompt.AppendLine("- ALL 7 DAYS: monday, tuesday, wednesday, thursday, friday, saturday, sunday");
            prompt.AppendLine("- 3 meals per day (breakfast, lunch, dinner) for EACH day");
            prompt.AppendLine("- Unique recipes with realistic ingredients for variety");
            prompt.AppendLine("- Balanced nutrition across the week");
            prompt.AppendLine("- Include estimated costs and cooking steps");
            prompt.AppendLine("- Use common cuisine types (American, Italian, Asian, Mediterranean, etc.)");
            prompt.AppendLine();
            prompt.AppendLine("IMPORTANT: Generate the complete JSON for all 7 days. Do not use shortcuts or placeholders!");

			return prompt.ToString();
        }


        public async Task<MealPlan> ParseMealPlanResponseAsync(string aiResponse, string userId, string weekIdentifier)
        {
            try
            {
                _logger.LogInformation("Parsing AI response for user {UserId}, response length: {Length}", userId, aiResponse.Length);

                // Clean up the response to extract JSON
                var jsonStart = aiResponse.IndexOf('{');
                var jsonEnd = aiResponse.LastIndexOf('}');
                
                if (jsonStart >= 0 && jsonEnd >= 0 && jsonEnd > jsonStart)
                {
                    var jsonContent = aiResponse[jsonStart..(jsonEnd + 1)];
                    _logger.LogInformation("Extracted JSON content, length: {Length}", jsonContent.Length);

                    try 
                    {
                        // Try to deserialize directly as MealPlan (new format)
                        var options = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                        };
                        
                        var mealPlan = JsonSerializer.Deserialize<MealPlan>(jsonContent, options);

                        if (mealPlan != null && !string.IsNullOrEmpty(mealPlan.Id))
                        {
                            _logger.LogInformation("Successfully parsed meal plan directly, ID: {Id}, Days: {DayCount}", 
                                mealPlan.Id, mealPlan.DailyMeals?.Count ?? 0);
                            
                            // Ensure required fields are set
                            mealPlan.UserId = userId;
                            mealPlan.WeekIdentifier = weekIdentifier;
                            mealPlan.WeekOf = Helpers.WeekHelper.GetMondayOfWeek(weekIdentifier);
                            mealPlan.Status = MealPlanStatus.Generated;
                            
                            // Initialize metadata if needed
                            mealPlan.GenerationMetadata ??= new GenerationMetadata
                            {
                                StartedAt = DateTime.UtcNow,
                                OrchestrationId = Guid.NewGuid().ToString()
                            };

                            return mealPlan;
                        }
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogWarning(ex, "Failed to parse as direct MealPlan, trying legacy format");
                        
                        // Fallback to legacy format
                        var aiMealPlan = JsonSerializer.Deserialize<AIMealPlanResponse>(jsonContent);
                        if (aiMealPlan != null)
                        {
                            return await ConvertAIResponseToMealPlan(aiMealPlan, userId, weekIdentifier);
                        }
                    }
                }
                
                _logger.LogWarning("Could not extract or parse JSON from AI response, using fallback");
                return CreateFallbackMealPlan(userId, weekIdentifier);
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
            var targetCals = userProfile.TargetCalories > 0 ? userProfile.TargetCalories : 2000;
            var userId = userProfile.UserId ?? "demo-user";
            
            // Generate a realistic mock meal plan that matches the expected schema for ALL 7 DAYS
            var mockPlan = $@"{{
  ""id"": ""{Guid.NewGuid()}"",
  ""userId"": ""{userId}"",
  ""weekIdentifier"": ""2025-W37"",
  ""dailyMeals"": {{
    ""monday"": {{
      ""date"": ""2025-09-08T00:00:00Z"",
      ""dayOfWeek"": ""monday"",
      ""meals"": {{
        ""breakfast"": {{
          ""id"": ""meal-mon-b"",
          ""name"": ""Oatmeal with Berries"",
          ""recipe"": {{
            ""id"": ""recipe-001"",
            ""name"": ""Oatmeal with Berries"",
            ""cuisine"": ""American"",
            ""steps"": [""Boil 1 cup water"", ""Add oats and cook 5 min"", ""Top with berries""],
            ""ingredients"": [
              {{""item"": ""rolled oats"", ""qty"": ""1/2"", ""unit"": ""cup"", ""category"": ""grains"", ""estimatedCost"": 0.5}},
              {{""item"": ""blueberries"", ""qty"": ""1/2"", ""unit"": ""cup"", ""category"": ""produce"", ""estimatedCost"": 2.0}}
            ],
            ""calories"": 320,
            ""difficulty"": ""easy"",
            ""totalTime"": 10,
            ""servings"": 1
          }},
          ""servings"": 1,
          ""mealType"": ""breakfast"",
          ""nutrition"": {{""calories"": 320, ""protein"": 12.0, ""carbohydrates"": 58.0, ""fats"": 6.0}}
        }},
        ""lunch"": {{
          ""id"": ""meal-mon-l"",
          ""name"": ""Turkey Sandwich"",
          ""recipe"": {{
            ""id"": ""recipe-002"",
            ""name"": ""Turkey Sandwich"",
            ""cuisine"": ""American"",
            ""steps"": [""Toast bread"", ""Add turkey and vegetables"", ""Serve""],
            ""ingredients"": [
              {{""item"": ""whole wheat bread"", ""qty"": ""2"", ""unit"": ""slices"", ""category"": ""grains"", ""estimatedCost"": 0.8}},
              {{""item"": ""turkey breast"", ""qty"": ""4"", ""unit"": ""oz"", ""category"": ""protein"", ""estimatedCost"": 3.0}}
            ],
            ""calories"": 380,
            ""difficulty"": ""easy"",
            ""totalTime"": 5,
            ""servings"": 1
          }},
          ""servings"": 1,
          ""mealType"": ""lunch"",
          ""nutrition"": {{""calories"": 380, ""protein"": 28.0, ""carbohydrates"": 32.0, ""fats"": 12.0}}
        }},
        ""dinner"": {{
          ""id"": ""meal-mon-d"",
          ""name"": ""Grilled Chicken & Rice"",
          ""recipe"": {{
            ""id"": ""recipe-003"",
            ""name"": ""Grilled Chicken & Rice"",
            ""cuisine"": ""American"",
            ""steps"": [""Season chicken"", ""Grill 6-8 min per side"", ""Cook rice"", ""Serve together""],
            ""ingredients"": [
              {{""item"": ""chicken breast"", ""qty"": ""6"", ""unit"": ""oz"", ""category"": ""protein"", ""estimatedCost"": 4.0}},
              {{""item"": ""brown rice"", ""qty"": ""1/2"", ""unit"": ""cup"", ""category"": ""grains"", ""estimatedCost"": 0.5}}
            ],
            ""calories"": 450,
            ""difficulty"": ""medium"",
            ""totalTime"": 25,
            ""servings"": 1
          }},
          ""servings"": 1,
          ""mealType"": ""dinner"",
          ""nutrition"": {{""calories"": 450, ""protein"": 42.0, ""carbohydrates"": 38.0, ""fats"": 8.0}}
        }}
      }}
    }},
    ""tuesday"": {{
      ""date"": ""2025-09-09T00:00:00Z"",
      ""dayOfWeek"": ""tuesday"",
      ""meals"": {{
        ""breakfast"": {{
          ""id"": ""meal-tue-b"",
          ""name"": ""Greek Yogurt Bowl"",
          ""recipe"": {{
            ""id"": ""recipe-004"",
            ""name"": ""Greek Yogurt Bowl"",
            ""cuisine"": ""Mediterranean"",
            ""steps"": [""Add yogurt to bowl"", ""Top with granola and fruit""],
            ""ingredients"": [
              {{""item"": ""greek yogurt"", ""qty"": ""1"", ""unit"": ""cup"", ""category"": ""dairy"", ""estimatedCost"": 1.5}},
              {{""item"": ""granola"", ""qty"": ""1/4"", ""unit"": ""cup"", ""category"": ""grains"", ""estimatedCost"": 1.0}}
            ],
            ""calories"": 300,
            ""difficulty"": ""easy"",
            ""totalTime"": 3,
            ""servings"": 1
          }},
          ""servings"": 1,
          ""mealType"": ""breakfast"",
          ""nutrition"": {{""calories"": 300, ""protein"": 20.0, ""carbohydrates"": 40.0, ""fats"": 8.0}}
        }},
        ""lunch"": {{
          ""id"": ""meal-tue-l"",
          ""name"": ""Caesar Salad"",
          ""recipe"": {{
            ""id"": ""recipe-005"",
            ""name"": ""Caesar Salad"",
            ""cuisine"": ""Italian"",
            ""steps"": [""Chop romaine"", ""Add dressing and toppings"", ""Toss and serve""],
            ""ingredients"": [
              {{""item"": ""romaine lettuce"", ""qty"": ""2"", ""unit"": ""cups"", ""category"": ""produce"", ""estimatedCost"": 1.0}},
              {{""item"": ""parmesan cheese"", ""qty"": ""1/4"", ""unit"": ""cup"", ""category"": ""dairy"", ""estimatedCost"": 1.5}}
            ],
            ""calories"": 350,
            ""difficulty"": ""easy"",
            ""totalTime"": 8,
            ""servings"": 1
          }},
          ""servings"": 1,
          ""mealType"": ""lunch"",
          ""nutrition"": {{""calories"": 350, ""protein"": 15.0, ""carbohydrates"": 20.0, ""fats"": 26.0}}
        }},
        ""dinner"": {{
          ""id"": ""meal-tue-d"",
          ""name"": ""Salmon & Vegetables"",
          ""recipe"": {{
            ""id"": ""recipe-006"",
            ""name"": ""Salmon & Vegetables"",
            ""cuisine"": ""American"",
            ""steps"": [""Season salmon"", ""Bake 12-15 min"", ""Steam vegetables"", ""Serve""],
            ""ingredients"": [
              {{""item"": ""salmon fillet"", ""qty"": ""6"", ""unit"": ""oz"", ""category"": ""protein"", ""estimatedCost"": 6.0}},
              {{""item"": ""asparagus"", ""qty"": ""1"", ""unit"": ""cup"", ""category"": ""produce"", ""estimatedCost"": 2.0}}
            ],
            ""calories"": 480,
            ""difficulty"": ""medium"",
            ""totalTime"": 20,
            ""servings"": 1
          }},
          ""servings"": 1,
          ""mealType"": ""dinner"",
          ""nutrition"": {{""calories"": 480, ""protein"": 38.0, ""carbohydrates"": 35.0, ""fats"": 18.0}}
        }}
      }}
    }},
    ""wednesday"": {{
      ""date"": ""2025-09-10T00:00:00Z"",
      ""dayOfWeek"": ""wednesday"",
      ""meals"": {{
        ""breakfast"": {{
          ""id"": ""meal-wed-b"",
          ""name"": ""Scrambled Eggs & Toast"",
          ""recipe"": {{
            ""id"": ""recipe-007"",
            ""name"": ""Scrambled Eggs & Toast"",
            ""cuisine"": ""American"",
            ""steps"": [""Beat eggs"", ""Cook in pan"", ""Toast bread"", ""Serve together""],
            ""ingredients"": [
              {{""item"": ""eggs"", ""qty"": ""2"", ""unit"": ""large"", ""category"": ""protein"", ""estimatedCost"": 1.0}},
              {{""item"": ""whole grain bread"", ""qty"": ""2"", ""unit"": ""slices"", ""category"": ""grains"", ""estimatedCost"": 0.8}}
            ],
            ""calories"": 340,
            ""difficulty"": ""easy"",
            ""totalTime"": 8,
            ""servings"": 1
          }},
          ""servings"": 1,
          ""mealType"": ""breakfast"",
          ""nutrition"": {{""calories"": 340, ""protein"": 18.0, ""carbohydrates"": 28.0, ""fats"": 16.0}}
        }},
        ""lunch"": {{
          ""id"": ""meal-wed-l"",
          ""name"": ""Quinoa Salad"",
          ""recipe"": {{
            ""id"": ""recipe-008"",
            ""name"": ""Quinoa Salad"",
            ""cuisine"": ""Mediterranean"",
            ""steps"": [""Cook quinoa"", ""Mix with vegetables"", ""Add dressing""],
            ""ingredients"": [
              {{""item"": ""quinoa"", ""qty"": ""1/2"", ""unit"": ""cup"", ""category"": ""grains"", ""estimatedCost"": 1.2}},
              {{""item"": ""cucumber"", ""qty"": ""1"", ""unit"": ""medium"", ""category"": ""produce"", ""estimatedCost"": 0.8}}
            ],
            ""calories"": 360,
            ""difficulty"": ""easy"",
            ""totalTime"": 15,
            ""servings"": 1
          }},
          ""servings"": 1,
          ""mealType"": ""lunch"",
          ""nutrition"": {{""calories"": 360, ""protein"": 12.0, ""carbohydrates"": 58.0, ""fats"": 8.0}}
        }},
        ""dinner"": {{
          ""id"": ""meal-wed-d"",
          ""name"": ""Beef Stir Fry"",
          ""recipe"": {{
            ""id"": ""recipe-009"",
            ""name"": ""Beef Stir Fry"",
            ""cuisine"": ""Asian"",
            ""steps"": [""Slice beef"", ""Heat wok"", ""Stir fry beef and vegetables"", ""Serve with rice""],
            ""ingredients"": [
              {{""item"": ""beef strips"", ""qty"": ""5"", ""unit"": ""oz"", ""category"": ""protein"", ""estimatedCost"": 5.0}},
              {{""item"": ""bell peppers"", ""qty"": ""1"", ""unit"": ""cup"", ""category"": ""produce"", ""estimatedCost"": 1.5}}
            ],
            ""calories"": 420,
            ""difficulty"": ""medium"",
            ""totalTime"": 20,
            ""servings"": 1
          }},
          ""servings"": 1,
          ""mealType"": ""dinner"",
          ""nutrition"": {{""calories"": 420, ""protein"": 35.0, ""carbohydrates"": 25.0, ""fats"": 18.0}}
        }}
      }}
    }},
    ""thursday"": {{
      ""date"": ""2025-09-11T00:00:00Z"",
      ""dayOfWeek"": ""thursday"",
      ""meals"": {{
        ""breakfast"": {{
          ""id"": ""meal-thu-b"",
          ""name"": ""Smoothie Bowl"",
          ""recipe"": {{
            ""id"": ""recipe-010"",
            ""name"": ""Smoothie Bowl"",
            ""cuisine"": ""American"",
            ""steps"": [""Blend fruits"", ""Pour into bowl"", ""Top with granola""],
            ""ingredients"": [
              {{""item"": ""banana"", ""qty"": ""1"", ""unit"": ""large"", ""category"": ""produce"", ""estimatedCost"": 0.5}},
              {{""item"": ""berries"", ""qty"": ""1/2"", ""unit"": ""cup"", ""category"": ""produce"", ""estimatedCost"": 2.5}}
            ],
            ""calories"": 290,
            ""difficulty"": ""easy"",
            ""totalTime"": 5,
            ""servings"": 1
          }},
          ""servings"": 1,
          ""mealType"": ""breakfast"",
          ""nutrition"": {{""calories"": 290, ""protein"": 8.0, ""carbohydrates"": 62.0, ""fats"": 4.0}}
        }},
        ""lunch"": {{
          ""id"": ""meal-thu-l"",
          ""name"": ""Chicken Wrap"",
          ""recipe"": {{
            ""id"": ""recipe-011"",
            ""name"": ""Chicken Wrap"",
            ""cuisine"": ""American"",
            ""steps"": [""Grill chicken"", ""Wrap in tortilla"", ""Add vegetables""],
            ""ingredients"": [
              {{""item"": ""chicken breast"", ""qty"": ""4"", ""unit"": ""oz"", ""category"": ""protein"", ""estimatedCost"": 3.5}},
              {{""item"": ""tortilla"", ""qty"": ""1"", ""unit"": ""large"", ""category"": ""grains"", ""estimatedCost"": 0.8}}
            ],
            ""calories"": 390,
            ""difficulty"": ""medium"",
            ""totalTime"": 15,
            ""servings"": 1
          }},
          ""servings"": 1,
          ""mealType"": ""lunch"",
          ""nutrition"": {{""calories"": 390, ""protein"": 32.0, ""carbohydrates"": 35.0, ""fats"": 12.0}}
        }},
        ""dinner"": {{
          ""id"": ""meal-thu-d"",
          ""name"": ""Pork Chops & Potatoes"",
          ""recipe"": {{
            ""id"": ""recipe-012"",
            ""name"": ""Pork Chops & Potatoes"",
            ""cuisine"": ""American"",
            ""steps"": [""Season pork chops"", ""Pan fry"", ""Roast potatoes"", ""Serve together""],
            ""ingredients"": [
              {{""item"": ""pork chops"", ""qty"": ""1"", ""unit"": ""piece"", ""category"": ""protein"", ""estimatedCost"": 4.5}},
              {{""item"": ""potatoes"", ""qty"": ""2"", ""unit"": ""medium"", ""category"": ""produce"", ""estimatedCost"": 1.0}}
            ],
            ""calories"": 460,
            ""difficulty"": ""medium"",
            ""totalTime"": 30,
            ""servings"": 1
          }},
          ""servings"": 1,
          ""mealType"": ""dinner"",
          ""nutrition"": {{""calories"": 460, ""protein"": 36.0, ""carbohydrates"": 42.0, ""fats"": 16.0}}
        }}
      }}
    }},
    ""friday"": {{
      ""date"": ""2025-09-12T00:00:00Z"",
      ""dayOfWeek"": ""friday"",
      ""meals"": {{
        ""breakfast"": {{
          ""id"": ""meal-fri-b"",
          ""name"": ""Pancakes"",
          ""recipe"": {{
            ""id"": ""recipe-013"",
            ""name"": ""Pancakes"",
            ""cuisine"": ""American"",
            ""steps"": [""Mix batter"", ""Cook on griddle"", ""Serve with syrup""],
            ""ingredients"": [
              {{""item"": ""flour"", ""qty"": ""1"", ""unit"": ""cup"", ""category"": ""grains"", ""estimatedCost"": 0.5}},
              {{""item"": ""milk"", ""qty"": ""1"", ""unit"": ""cup"", ""category"": ""dairy"", ""estimatedCost"": 0.6}}
            ],
            ""calories"": 350,
            ""difficulty"": ""easy"",
            ""totalTime"": 15,
            ""servings"": 1
          }},
          ""servings"": 1,
          ""mealType"": ""breakfast"",
          ""nutrition"": {{""calories"": 350, ""protein"": 10.0, ""carbohydrates"": 65.0, ""fats"": 8.0}}
        }},
        ""lunch"": {{
          ""id"": ""meal-fri-l"",
          ""name"": ""Vegetable Soup"",
          ""recipe"": {{
            ""id"": ""recipe-014"",
            ""name"": ""Vegetable Soup"",
            ""cuisine"": ""American"",
            ""steps"": [""Chop vegetables"", ""Simmer in broth"", ""Season to taste""],
            ""ingredients"": [
              {{""item"": ""mixed vegetables"", ""qty"": ""2"", ""unit"": ""cups"", ""category"": ""produce"", ""estimatedCost"": 2.0}},
              {{""item"": ""vegetable broth"", ""qty"": ""2"", ""unit"": ""cups"", ""category"": ""pantry"", ""estimatedCost"": 1.0}}
            ],
            ""calories"": 180,
            ""difficulty"": ""easy"",
            ""totalTime"": 25,
            ""servings"": 1
          }},
          ""servings"": 1,
          ""mealType"": ""lunch"",
          ""nutrition"": {{""calories"": 180, ""protein"": 6.0, ""carbohydrates"": 35.0, ""fats"": 2.0}}
        }},
        ""dinner"": {{
          ""id"": ""meal-fri-d"",
          ""name"": ""Fish Tacos"",
          ""recipe"": {{
            ""id"": ""recipe-015"",
            ""name"": ""Fish Tacos"",
            ""cuisine"": ""Mexican"",
            ""steps"": [""Season fish"", ""Cook fish"", ""Warm tortillas"", ""Assemble tacos""],
            ""ingredients"": [
              {{""item"": ""white fish"", ""qty"": ""5"", ""unit"": ""oz"", ""category"": ""protein"", ""estimatedCost"": 5.5}},
              {{""item"": ""corn tortillas"", ""qty"": ""3"", ""unit"": ""pieces"", ""category"": ""grains"", ""estimatedCost"": 1.0}}
            ],
            ""calories"": 480,
            ""difficulty"": ""medium"",
            ""totalTime"": 20,
            ""servings"": 1
          }},
          ""servings"": 1,
          ""mealType"": ""dinner"",
          ""nutrition"": {{""calories"": 480, ""protein"": 35.0, ""carbohydrates"": 45.0, ""fats"": 16.0}}
        }}
      }}
    }},
    ""saturday"": {{
      ""date"": ""2025-09-13T00:00:00Z"",
      ""dayOfWeek"": ""saturday"",
      ""meals"": {{
        ""breakfast"": {{
          ""id"": ""meal-sat-b"",
          ""name"": ""Avocado Toast"",
          ""recipe"": {{
            ""id"": ""recipe-016"",
            ""name"": ""Avocado Toast"",
            ""cuisine"": ""American"",
            ""steps"": [""Toast bread"", ""Mash avocado"", ""Spread on toast"", ""Season""],
            ""ingredients"": [
              {{""item"": ""avocado"", ""qty"": ""1"", ""unit"": ""large"", ""category"": ""produce"", ""estimatedCost"": 1.5}},
              {{""item"": ""sourdough bread"", ""qty"": ""2"", ""unit"": ""slices"", ""category"": ""grains"", ""estimatedCost"": 1.0}}
            ],
            ""calories"": 330,
            ""difficulty"": ""easy"",
            ""totalTime"": 5,
            ""servings"": 1
          }},
          ""servings"": 1,
          ""mealType"": ""breakfast"",
          ""nutrition"": {{""calories"": 330, ""protein"": 8.0, ""carbohydrates"": 35.0, ""fats"": 18.0}}
        }},
        ""lunch"": {{
          ""id"": ""meal-sat-l"",
          ""name"": ""Pasta Salad"",
          ""recipe"": {{
            ""id"": ""recipe-017"",
            ""name"": ""Pasta Salad"",
            ""cuisine"": ""Italian"",
            ""steps"": [""Cook pasta"", ""Mix with vegetables"", ""Add dressing""],
            ""ingredients"": [
              {{""item"": ""pasta"", ""qty"": ""1"", ""unit"": ""cup"", ""category"": ""grains"", ""estimatedCost"": 0.8}},
              {{""item"": ""cherry tomatoes"", ""qty"": ""1"", ""unit"": ""cup"", ""category"": ""produce"", ""estimatedCost"": 1.8}}
            ],
            ""calories"": 380,
            ""difficulty"": ""easy"",
            ""totalTime"": 12,
            ""servings"": 1
          }},
          ""servings"": 1,
          ""mealType"": ""lunch"",
          ""nutrition"": {{""calories"": 380, ""protein"": 12.0, ""carbohydrates"": 68.0, ""fats"": 8.0}}
        }},
        ""dinner"": {{
          ""id"": ""meal-sat-d"",
          ""name"": ""BBQ Ribs"",
          ""recipe"": {{
            ""id"": ""recipe-018"",
            ""name"": ""BBQ Ribs"",
            ""cuisine"": ""American"",
            ""steps"": [""Season ribs"", ""Slow cook"", ""Baste with sauce"", ""Finish on grill""],
            ""ingredients"": [
              {{""item"": ""pork ribs"", ""qty"": ""8"", ""unit"": ""oz"", ""category"": ""protein"", ""estimatedCost"": 6.0}},
              {{""item"": ""BBQ sauce"", ""qty"": ""1/4"", ""unit"": ""cup"", ""category"": ""pantry"", ""estimatedCost"": 0.5}}
            ],
            ""calories"": 520,
            ""difficulty"": ""hard"",
            ""totalTime"": 180,
            ""servings"": 1
          }},
          ""servings"": 1,
          ""mealType"": ""dinner"",
          ""nutrition"": {{""calories"": 520, ""protein"": 45.0, ""carbohydrates"": 15.0, ""fats"": 32.0}}
        }}
      }}
    }},
    ""sunday"": {{
      ""date"": ""2025-09-14T00:00:00Z"",
      ""dayOfWeek"": ""sunday"",
      ""meals"": {{
        ""breakfast"": {{
          ""id"": ""meal-sun-b"",
          ""name"": ""French Toast"",
          ""recipe"": {{
            ""id"": ""recipe-019"",
            ""name"": ""French Toast"",
            ""cuisine"": ""French"",
            ""steps"": [""Beat eggs and milk"", ""Dip bread"", ""Cook in pan"", ""Serve with syrup""],
            ""ingredients"": [
              {{""item"": ""brioche bread"", ""qty"": ""3"", ""unit"": ""slices"", ""category"": ""grains"", ""estimatedCost"": 1.5}},
              {{""item"": ""eggs"", ""qty"": ""2"", ""unit"": ""large"", ""category"": ""protein"", ""estimatedCost"": 1.0}}
            ],
            ""calories"": 380,
            ""difficulty"": ""medium"",
            ""totalTime"": 12,
            ""servings"": 1
          }},
          ""servings"": 1,
          ""mealType"": ""breakfast"",
          ""nutrition"": {{""calories"": 380, ""protein"": 16.0, ""carbohydrates"": 48.0, ""fats"": 14.0}}
        }},
        ""lunch"": {{
          ""id"": ""meal-sun-l"",
          ""name"": ""Chicken Salad"",
          ""recipe"": {{
            ""id"": ""recipe-020"",
            ""name"": ""Chicken Salad"",
            ""cuisine"": ""American"",
            ""steps"": [""Grill chicken"", ""Slice chicken"", ""Mix with greens"", ""Add dressing""],
            ""ingredients"": [
              {{""item"": ""chicken breast"", ""qty"": ""5"", ""unit"": ""oz"", ""category"": ""protein"", ""estimatedCost"": 4.0}},
              {{""item"": ""mixed greens"", ""qty"": ""2"", ""unit"": ""cups"", ""category"": ""produce"", ""estimatedCost"": 1.5}}
            ],
            ""calories"": 320,
            ""difficulty"": ""easy"",
            ""totalTime"": 15,
            ""servings"": 1
          }},
          ""servings"": 1,
          ""mealType"": ""lunch"",
          ""nutrition"": {{""calories"": 320, ""protein"": 38.0, ""carbohydrates"": 12.0, ""fats"": 12.0}}
        }},
        ""dinner"": {{
          ""id"": ""meal-sun-d"",
          ""name"": ""Spaghetti Marinara"",
          ""recipe"": {{
            ""id"": ""recipe-021"",
            ""name"": ""Spaghetti Marinara"",
            ""cuisine"": ""Italian"",
            ""steps"": [""Boil pasta"", ""Heat marinara sauce"", ""Combine"", ""Serve with parmesan""],
            ""ingredients"": [
              {{""item"": ""spaghetti"", ""qty"": ""4"", ""unit"": ""oz"", ""category"": ""grains"", ""estimatedCost"": 0.8}},
              {{""item"": ""marinara sauce"", ""qty"": ""1"", ""unit"": ""cup"", ""category"": ""pantry"", ""estimatedCost"": 1.2}}
            ],
            ""calories"": 420,
            ""difficulty"": ""easy"",
            ""totalTime"": 15,
            ""servings"": 1
          }},
          ""servings"": 1,
          ""mealType"": ""dinner"",
          ""nutrition"": {{""calories"": 420, ""protein"": 14.0, ""carbohydrates"": 78.0, ""fats"": 6.0}}
        }}
      }}
    }}
  }}
}}";
            
            return mockPlan;
        }
    }
}
