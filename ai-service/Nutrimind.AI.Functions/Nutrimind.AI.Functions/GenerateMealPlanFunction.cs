using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NutriMind.Functions.Models;
using NutriMind.Functions.Services;
using System.Net;

namespace NutriMind.Functions
{
	public class GenerateMealPlanFunction
	{
		private readonly ILogger _logger;
		private readonly IConfiguration _config;

		public GenerateMealPlanFunction(ILoggerFactory loggerFactory, IConfiguration config)
		{
			_logger = loggerFactory.CreateLogger<GenerateMealPlanFunction>();
			_config = config;
		}

		[Function("GenerateMealPlan")]
		public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req)
		{
			var body = await new StreamReader(req.Body).ReadToEndAsync();
			var input = JsonConvert.DeserializeObject<UserInput>(body);

			var keyVaultUri = _config["KeyVaultUri"];
			var kvClient = new SecretClient(new Uri(keyVaultUri), new DefaultAzureCredential());

			var openAiKey = kvClient.GetSecret("openai-api-key").Value.Value;
			var openAiEndpoint = kvClient.GetSecret("openai-endpoint").Value.Value;
			var searchKey = kvClient.GetSecret("ai-search-key").Value.Value;
			var searchEndpoint = _config["SearchEndpoint"];
			var openAiDeployment = _config["OpenAiDeployment"];
			var schemaPath = Path.Combine(Environment.CurrentDirectory, "Models", "MealPlanSchema.json");

			var searchService = new SearchService(searchEndpoint, searchKey);
			var recipes = await searchService.QueryRecipesAsync(input);

			var openAiService = new OpenAIService(openAiEndpoint, openAiKey, openAiDeployment, schemaPath);
			var plan = await openAiService.GenerateMealPlanAsync(input, recipes);

			if (!openAiService.ValidateMealPlan(plan, out var errors))
			{
				_logger.LogError("Invalid meal plan: {errors}", string.Join(", ", errors));
				return req.CreateResponse(HttpStatusCode.BadRequest);
			}

			var response = req.CreateResponse(HttpStatusCode.OK);
			response.Headers.Add("Content-Type", "application/json");
			await response.WriteStringAsync(plan);

			return response;
		}
	}
}
