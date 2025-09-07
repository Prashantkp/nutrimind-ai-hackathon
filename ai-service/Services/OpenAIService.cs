using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure.AI.OpenAI;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Schema;
using NutriMind.Functions.Models;

namespace NutriMind.Functions.Services
{
    /// <summary>
    /// Service for generating meal plans using Azure OpenAI and validating against JSON schema
    /// </summary>
    public class OpenAIService
    {
        private readonly string _openAIEndpoint;
        private readonly string _deploymentName;
        private readonly string _keyVaultUrl;
        private readonly ILogger _logger;
        private OpenAIClient? _openAIClient;
        private JSchema? _mealPlanSchema;

        public OpenAIService(string openAIEndpoint, string deploymentName, string keyVaultUrl, ILogger logger)
        {
            _openAIEndpoint = openAIEndpoint ?? throw new ArgumentNullException(nameof(openAIEndpoint));
            _deploymentName = deploymentName ?? throw new ArgumentNullException(nameof(deploymentName));
            _keyVaultUrl = keyVaultUrl ?? throw new ArgumentNullException(nameof(keyVaultUrl));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Initializes the OpenAI client with credentials from Key Vault
        /// </summary>
        private async Task InitializeClientAsync()
        {
            if (_openAIClient != null)
                return;

            try
            {
                _logger.LogInformation("Initializing OpenAI client...");

                // Get OpenAI API key from Key Vault using managed identity
                var credential = new DefaultAzureCredential();
                var keyVaultClient = new SecretClient(new Uri(_keyVaultUrl), credential);
                
                var openAIKeySecret = await keyVaultClient.GetSecretAsync("azure-openai-api-key");
                var openAIApiKey = openAIKeySecret.Value.Value;

                _logger.LogInformation("Retrieved OpenAI API key from Key Vault");

                // Create OpenAI client
                _openAIClient = new OpenAIClient(new Uri(_openAIEndpoint), new Azure.AzureKeyCredential(openAIApiKey));

                _logger.LogInformation("OpenAI client initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize OpenAI client");
                throw;
            }
        }

        /// <summary>
        /// Loads and parses the meal plan JSON schema for validation
        /// </summary>
        private async Task LoadSchemaAsync()
        {
            if (_mealPlanSchema != null)
                return;

            try
            {
                _logger.LogInformation("Loading meal plan JSON schema...");

                // Load schema from embedded resource or file
                var schemaPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Models", "MealPlanSchema.json");
                var schemaJson = await File.ReadAllTextAsync(schemaPath);
                
                _mealPlanSchema = JSchema.Parse(schemaJson);
                
                _logger.LogInformation("Meal plan schema loaded successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load meal plan JSON schema");
                throw;
            }
        }

        /// <summary>
        /// Generates a structured meal plan using Azure OpenAI
        /// </summary>
        /// <param name="userInput">User's preferences and constraints</param>
        /// <param name="recipes">Available recipes from search</param>
        /// <returns>Generated meal plan as JSON object</returns>
        public async Task<object?> GenerateMealPlanAsync(UserInput userInput, List<Recipe> recipes)
        {
            await InitializeClientAsync();
            await LoadSchemaAsync();

            if (_openAIClient == null)
            {
                throw new InvalidOperationException("OpenAI client is not initialized");
            }

            try
            {
                _logger.LogInformation("Generating meal plan with OpenAI...");

                // Build the system prompt with schema and requirements
                var systemPrompt = BuildSystemPrompt(userInput);
                
                // Build the user prompt with recipes and user preferences
                var userPrompt = BuildUserPrompt(userInput, recipes);

                _logger.LogInformation($"System prompt length: {systemPrompt.Length}, User prompt length: {userPrompt.Length}");

                // Create chat completion request
                var chatCompletionsOptions = new ChatCompletionsOptions(_deploymentName, new ChatRequestMessage[]
                {
                    new ChatRequestSystemMessage(systemPrompt),
                    new ChatRequestUserMessage(userPrompt)
                })
                {
                    Temperature = 0.7f,
                    MaxTokens = 4000,
                    NucleusSamplingFactor = 0.95f,
                    FrequencyPenalty = 0.0f,
                    PresencePenalty = 0.0f
                };

                // Get completion from OpenAI
                var response = await _openAIClient.GetChatCompletionsAsync(chatCompletionsOptions);
                
                if (response?.Value?.Choices == null || !response.Value.Choices.Any())
                {
                    _logger.LogError("No response received from OpenAI");
                    return null;
                }

                var completionText = response.Value.Choices[0].Message.Content;
                _logger.LogInformation($"Received OpenAI response: {completionText?.Length ?? 0} characters");

                if (string.IsNullOrWhiteSpace(completionText))
                {
                    _logger.LogError("Empty response from OpenAI");
                    return null;
                }

                // Parse and validate the JSON response
                var mealPlan = await ValidateAndParseMealPlanAsync(completionText);
                
                if (mealPlan == null)
                {
                    _logger.LogError("Failed to validate meal plan against schema");
                    return null;
                }

                _logger.LogInformation("Successfully generated and validated meal plan");
                return mealPlan;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while generating meal plan");
                throw;
            }
        }

        /// <summary>
        /// Builds the system prompt with instructions and schema
        /// </summary>
        private string BuildSystemPrompt(UserInput userInput)
        {
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
            prompt.AppendLine($"- Number of meals per day: {userInput.MealsPerDay}");
            prompt.AppendLine($"- Number of days to plan: {userInput.PlanDays}");
            
            if (userInput.Allergens?.Any() == true)
            {
                prompt.AppendLine($"- Allergens to avoid: {string.Join(", ", userInput.Allergens)}");
            }
            
            if (userInput.Dislikes?.Any() == true)
            {
                prompt.AppendLine($"- Dislikes to avoid: {string.Join(", ", userInput.Dislikes)}");
            }

            if (!string.IsNullOrWhiteSpace(userInput.AdditionalNotes))
            {
                prompt.AppendLine($"- Additional notes: {userInput.AdditionalNotes}");
            }

            prompt.AppendLine();
            prompt.AppendLine("AVAILABLE RECIPES:");
            
            // Group recipes by meal type for better organization
            var recipesByMealType = recipes.GroupBy(r => r.MealType).ToList();
            
            foreach (var mealGroup in recipesByMealType)
            {
                prompt.AppendLine($"\n{mealGroup.Key.ToUpper()} RECIPES:");
                
                foreach (var recipe in mealGroup.Take(15)) // Limit to prevent token overflow
                {
                    prompt.AppendLine($"\nRecipe ID: {recipe.Id}");
                    prompt.AppendLine($"Title: {recipe.Title}");
                    prompt.AppendLine($"Description: {recipe.Description}");
                    prompt.AppendLine($"Calories per serving: {recipe.CaloriesPerServing}");
                    prompt.AppendLine($"Servings: {recipe.Servings}");
                    prompt.AppendLine($"Prep time: {recipe.PrepTimeMinutes} min");
                    prompt.AppendLine($"Cook time: {recipe.CookTimeMinutes} min");
                    prompt.AppendLine($"Difficulty: {recipe.Difficulty}");
                    prompt.AppendLine($"Cuisine: {recipe.Cuisine}");
                    
                    if (recipe.DietaryTags?.Any() == true)
                    {
                        prompt.AppendLine($"Dietary tags: {string.Join(", ", recipe.DietaryTags)}");
                    }
                    
                    if (recipe.Ingredients?.Any() == true)
                    {
                        prompt.AppendLine($"Key ingredients: {string.Join(", ", recipe.Ingredients.Take(5))}");
                    }
                    
                    prompt.AppendLine($"Nutrition - Protein: {recipe.Nutrition.Protein}g, Carbs: {recipe.Nutrition.Carbohydrates}g, Fat: {recipe.Nutrition.Fat}g");
                }
            }

            prompt.AppendLine();
            prompt.AppendLine("Create a balanced meal plan using these recipes. Ensure variety, nutritional balance, and adherence to all user preferences.");

            return prompt.ToString();
        }

        /// <summary>
        /// Validates the OpenAI response against the JSON schema and parses it
        /// </summary>
        private async Task<object?> ValidateAndParseMealPlanAsync(string jsonResponse)
        {
            try
            {
                _logger.LogInformation("Validating meal plan JSON response...");

                // Clean the response - remove any markdown formatting or extra text
                var cleanedJson = CleanJsonResponse(jsonResponse);
                
                // Parse JSON
                var jsonObject = JsonConvert.DeserializeObject(cleanedJson);
                
                if (jsonObject == null)
                {
                    _logger.LogError("Failed to parse JSON response");
                    return null;
                }

                // Validate against schema
                if (_mealPlanSchema != null)
                {
                    var jObject = Newtonsoft.Json.Linq.JObject.Parse(cleanedJson);
                    
                    if (!jObject.IsValid(_mealPlanSchema, out IList<string> errorMessages))
                    {
                        _logger.LogError($"JSON validation failed. Errors: {string.Join(", ", errorMessages)}");
                        
                        // Log the problematic JSON for debugging
                        _logger.LogWarning($"Invalid JSON: {cleanedJson}");
                        
                        return null;
                    }
                }

                _logger.LogInformation("Meal plan JSON validated successfully");
                return jsonObject;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, $"JSON parsing error: {ex.Message}");
                _logger.LogWarning($"Problematic JSON response: {jsonResponse}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating meal plan JSON");
                return null;
            }
        }

        /// <summary>
        /// Cleans the JSON response by removing markdown formatting and extra text
        /// </summary>
        private string CleanJsonResponse(string response)
        {
            if (string.IsNullOrWhiteSpace(response))
                return string.Empty;

            // Remove markdown code block formatting
            response = response.Trim();
            
            // Remove ```json and ``` markers
            if (response.StartsWith("```json"))
            {
                response = response.Substring(7);
            }
            else if (response.StartsWith("```"))
            {
                response = response.Substring(3);
            }
            
            if (response.EndsWith("```"))
            {
                response = response.Substring(0, response.Length - 3);
            }
            
            // Find the first { and last } to extract just the JSON object
            var firstBrace = response.IndexOf('{');
            var lastBrace = response.LastIndexOf('}');
            
            if (firstBrace >= 0 && lastBrace >= 0 && lastBrace > firstBrace)
            {
                response = response.Substring(firstBrace, lastBrace - firstBrace + 1);
            }
            
            return response.Trim();
        }
    }
}
