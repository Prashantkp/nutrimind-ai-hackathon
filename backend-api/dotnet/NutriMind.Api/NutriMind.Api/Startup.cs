using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NutriMind.Api.Data;
using NutriMind.Api.Services;
using Azure.Security.KeyVault.Secrets;
using Azure.Identity;
using System;

[assembly: FunctionsStartup(typeof(NutriMind.Api.Startup))]

namespace NutriMind.Api
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            var configuration = builder.GetContext().Configuration;

            // Configure Key Vault client
            var keyVaultUrl = configuration["KeyVaultUrl"] ?? "https://kv-nutrimind-ai.vault.azure.net/";
            var credential = new DefaultAzureCredential();
            var keyVaultClient = new SecretClient(new Uri(keyVaultUrl), credential);

            // Get connection string from Key Vault
            try
            {
                var connectionStringSecret = keyVaultClient.GetSecret("sql-connection-string");
                var connectionString = connectionStringSecret.Value.Value;

                // Register DbContext with SQL Server
                builder.Services.AddDbContext<NutriMindDbContext>(options =>
                    options.UseSqlServer(connectionString));
            }
            catch (Exception)
            {
                // Fallback to configuration if Key Vault is not available
                var connectionString = configuration.GetConnectionString("DefaultConnection") 
                    ?? "Server=sqlsrv-nutrimind-ai-eastus2.database.windows.net;Database=sqldb-nutrimind-ai;Trusted_Connection=false;MultipleActiveResultSets=true;Encrypt=true;TrustServerCertificate=false;Connection Timeout=30;";
                
                builder.Services.AddDbContext<NutriMindDbContext>(options =>
                    options.UseSqlServer(connectionString));
            }

            // Register services
            builder.Services.AddScoped<IUserProfileService, UserProfileService>();
            builder.Services.AddScoped<IMealPlanService, MealPlanService>();
            builder.Services.AddScoped<IRecipeService, RecipeService>();
            builder.Services.AddScoped<IGroceryService, GroceryService>();
            builder.Services.AddScoped<INotificationService, NotificationService>();
            builder.Services.AddScoped<IIntegrationService, IntegrationService>();
            builder.Services.AddScoped<IAuthService, AuthService>();

            // Configure OpenAI service
            try
            {
                var openAiEndpoint = keyVaultClient.GetSecret("openai-endpoint").Value.Value;
                var openAiApiKey = keyVaultClient.GetSecret("openai-api-key").Value.Value;
                
                builder.Services.AddSingleton<IOpenAIService>(provider => 
                    new OpenAIService(openAiEndpoint, openAiApiKey));
            }
            catch (Exception)
            {
                // Fallback to mock service
                builder.Services.AddSingleton<IOpenAIService, OpenAIService>();
            }

            // Configure Azure AI Search service
            try
            {
                var searchEndpoint = configuration["SearchEndpoint"] ?? "https://srch-nutrimind-ai-hackathon-eastus2.search.windows.net";
                var searchApiKey = keyVaultClient.GetSecret("ai-search-key").Value.Value;
                
                builder.Services.AddSingleton<ISearchService>(provider => 
                    new SearchService(searchEndpoint, searchApiKey));
            }
            catch (Exception)
            {
                // Fallback to mock service
                builder.Services.AddSingleton<ISearchService, SearchService>();
            }
        }
    }
}
