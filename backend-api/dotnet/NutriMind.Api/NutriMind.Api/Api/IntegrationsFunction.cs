using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using NutriMind.Api.Models;
using NutriMind.Api.Services;
using System.Net;
using Newtonsoft.Json;

namespace NutriMind.Api.Api
{
    public class IntegrationsFunction
    {
        private readonly ILogger<IntegrationsFunction> _logger;
        private readonly IAuthService _authService;
        private readonly HttpClient _httpClient;

        // OAuth configuration for different providers
        private readonly Dictionary<string, OAuthConfig> _oauthConfigs = new()
        {
            ["instacart"] = new OAuthConfig
            {
                ClientId = Environment.GetEnvironmentVariable("INSTACART_CLIENT_ID") ?? "demo_client_id",
                ClientSecret = Environment.GetEnvironmentVariable("INSTACART_CLIENT_SECRET") ?? "demo_client_secret",
                AuthorizationUrl = "https://www.instacart.com/oauth/authorize",
                TokenUrl = "https://www.instacart.com/oauth/access_token",
                Scopes = new[] { "delivery_api:read", "delivery_api:write" }
            },
            ["amazon_fresh"] = new OAuthConfig
            {
                ClientId = Environment.GetEnvironmentVariable("AMAZON_FRESH_CLIENT_ID") ?? "demo_client_id",
                ClientSecret = Environment.GetEnvironmentVariable("AMAZON_FRESH_CLIENT_SECRET") ?? "demo_client_secret",
                AuthorizationUrl = "https://api.amazonalexa.com/v1/oauth2/authorize",
                TokenUrl = "https://api.amazonalexa.com/v1/oauth2/token",
                Scopes = new[] { "alexa:grocery_api" }
            },
            ["walmart_grocery"] = new OAuthConfig
            {
                ClientId = Environment.GetEnvironmentVariable("WALMART_CLIENT_ID") ?? "demo_client_id",
                ClientSecret = Environment.GetEnvironmentVariable("WALMART_CLIENT_SECRET") ?? "demo_client_secret",
                AuthorizationUrl = "https://developer.api.walmart.com/oauth2/authorize",
                TokenUrl = "https://developer.api.walmart.com/oauth2/token",
                Scopes = new[] { "grocery_api" }
            },
            ["kroger"] = new OAuthConfig
            {
                ClientId = Environment.GetEnvironmentVariable("KROGER_CLIENT_ID") ?? "demo_client_id",
                ClientSecret = Environment.GetEnvironmentVariable("KROGER_CLIENT_SECRET") ?? "demo_client_secret",
                AuthorizationUrl = "https://api.kroger.com/v1/oauth2/authorize",
                TokenUrl = "https://api.kroger.com/v1/oauth2/token",
                Scopes = new[] { "product.compact", "cart.basic:write" }
            }
        };

        public IntegrationsFunction(
            ILogger<IntegrationsFunction> logger,
            IAuthService authService,
            HttpClient httpClient)
        {
            _logger = logger;
            _authService = authService;
            _httpClient = httpClient;
        }

        [Function("ConnectProvider")]
        public async Task<HttpResponseData> ConnectProvider(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "integrations/{provider}/connect")] HttpRequestData req,
            string provider)
        {
            try
            {
                _logger.LogInformation("Initiating OAuth flow for provider: {Provider}", provider);

                // Get user ID from authentication
                var userId = _authService.GetUserIdFromRequest(req);
                if (string.IsNullOrEmpty(userId))
                {
                    return await CreateErrorResponse(req, HttpStatusCode.Unauthorized, "Authentication required");
                }

                // Read request body
                var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var connectRequest = JsonConvert.DeserializeObject<ConnectProviderRequest>(requestBody);

                if (connectRequest == null || string.IsNullOrEmpty(connectRequest.RedirectUri))
                {
                    return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Invalid request body or missing redirect URI");
                }

                // Validate provider
                if (!_oauthConfigs.ContainsKey(provider.ToLower()))
                {
                    return await CreateErrorResponse(req, HttpStatusCode.BadRequest, $"Unsupported provider: {provider}");
                }

                var config = _oauthConfigs[provider.ToLower()];
                var state = Guid.NewGuid().ToString(); // Generate unique state for security

                // Store state temporarily (in production, use Redis or similar)
                // For demo, we'll just return it

                // Build authorization URL
                var authUrl = $"{config.AuthorizationUrl}?" +
                             $"client_id={config.ClientId}&" +
                             $"redirect_uri={Uri.EscapeDataString(connectRequest.RedirectUri)}&" +
                             $"scope={Uri.EscapeDataString(string.Join(" ", config.Scopes))}&" +
                             $"state={state}&" +
                             $"response_type=code";

                var response = new OAuthInitiationResponse
                {
                    AuthorizationUrl = authUrl,
                    State = state
                };

                return await CreateSuccessResponse(req, response, $"OAuth flow initiated for {provider}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating OAuth flow for provider: {Provider}", provider);
                return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "Internal server error");
            }
        }

        [Function("HandleOAuthCallback")]
        public async Task<HttpResponseData> HandleOAuthCallback(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "integrations/{provider}/callback")] HttpRequestData req,
            string provider)
        {
            try
            {
                _logger.LogInformation("Handling OAuth callback for provider: {Provider}", provider);

                // Get user ID from authentication
                var userId = _authService.GetUserIdFromRequest(req);
                if (string.IsNullOrEmpty(userId))
                {
                    return await CreateErrorResponse(req, HttpStatusCode.Unauthorized, "Authentication required");
                }

                // Read request body
                var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var callbackRequest = JsonConvert.DeserializeObject<OAuthCallbackRequest>(requestBody);

                if (callbackRequest == null || string.IsNullOrEmpty(callbackRequest.Code))
                {
                    return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Invalid request body or missing authorization code");
                }

                // Validate provider
                if (!_oauthConfigs.ContainsKey(provider.ToLower()))
                {
                    return await CreateErrorResponse(req, HttpStatusCode.BadRequest, $"Unsupported provider: {provider}");
                }

                var config = _oauthConfigs[provider.ToLower()];

                // Exchange authorization code for access token
                var tokenResponse = await ExchangeCodeForToken(config, callbackRequest.Code);

                if (tokenResponse == null)
                {
                    return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Failed to exchange authorization code for token");
                }

                // Store the tokens in user profile (simplified - in production, encrypt tokens)
                await UpdateUserIntegration(userId, provider.ToLower(), tokenResponse);

                var result = new
                {
                    provider = provider,
                    connected = true,
                    connectedAt = DateTime.UtcNow
                };

                return await CreateSuccessResponse(req, result, $"Successfully connected to {provider}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling OAuth callback for provider: {Provider}", provider);
                return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "Internal server error");
            }
        }

        [Function("DisconnectProvider")]
        public async Task<HttpResponseData> DisconnectProvider(
            [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "integrations/{provider}/disconnect")] HttpRequestData req,
            string provider)
        {
            try
            {
                _logger.LogInformation("Disconnecting provider: {Provider}", provider);

                // Get user ID from authentication
                var userId = _authService.GetUserIdFromRequest(req);
                if (string.IsNullOrEmpty(userId))
                {
                    return await CreateErrorResponse(req, HttpStatusCode.Unauthorized, "Authentication required");
                }

                // Validate provider
                if (!_oauthConfigs.ContainsKey(provider.ToLower()))
                {
                    return await CreateErrorResponse(req, HttpStatusCode.BadRequest, $"Unsupported provider: {provider}");
                }

                // Remove integration from user profile
                await RemoveUserIntegration(userId, provider.ToLower());

                var result = new
                {
                    provider = provider,
                    disconnected = true,
                    disconnectedAt = DateTime.UtcNow
                };

                return await CreateSuccessResponse(req, result, $"Successfully disconnected from {provider}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disconnecting provider: {Provider}", provider);
                return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "Internal server error");
            }
        }

        [Function("GetIntegrations")]
        public async Task<HttpResponseData> GetIntegrations(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "integrations")] HttpRequestData req)
        {
            try
            {
                _logger.LogInformation("Getting user integrations");

                // Get user ID from authentication
                var userId = _authService.GetUserIdFromRequest(req);
                if (string.IsNullOrEmpty(userId))
                {
                    return await CreateErrorResponse(req, HttpStatusCode.Unauthorized, "Authentication required");
                }

                // Get user integrations (this would typically come from the user profile)
                var integrations = await GetUserIntegrations(userId);

                return await CreateSuccessResponse(req, integrations, "Integrations retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user integrations");
                return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "Internal server error");
            }
        }

        private async Task<TokenResponse?> ExchangeCodeForToken(OAuthConfig config, string authorizationCode)
        {
            try
            {
                var requestData = new List<KeyValuePair<string, string>>
                {
                    new("grant_type", "authorization_code"),
                    new("client_id", config.ClientId),
                    new("client_secret", config.ClientSecret),
                    new("code", authorizationCode)
                };

                var content = new FormUrlEncodedContent(requestData);
                var response = await _httpClient.PostAsync(config.TokenUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<TokenResponse>(json);
                }

                _logger.LogWarning("Token exchange failed with status: {StatusCode}", response.StatusCode);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exchanging authorization code for token");
                return null;
            }
        }

        private async Task UpdateUserIntegration(string userId, string provider, TokenResponse tokenResponse)
        {
            // In a real implementation, you would update the user profile in Cosmos DB
            // For demo purposes, we'll just log
            _logger.LogInformation("Would update integration for user {UserId}, provider {Provider}", userId, provider);
            
            // This would be:
            // 1. Load user profile from Cosmos DB
            // 2. Update the ConnectedServices with the new token info
            // 3. Save back to Cosmos DB
        }

        private async Task RemoveUserIntegration(string userId, string provider)
        {
            // In a real implementation, you would remove the integration from user profile
            _logger.LogInformation("Would remove integration for user {UserId}, provider {Provider}", userId, provider);
        }

        private async Task<object> GetUserIntegrations(string userId)
        {
            // In a real implementation, you would load from user profile
            // For demo, return mock data
            return new
            {
                instacart = new { isConnected = false },
                amazon_fresh = new { isConnected = false },
                walmart_grocery = new { isConnected = false },
                kroger = new { isConnected = false }
            };
        }

        private async Task<HttpResponseData> CreateSuccessResponse<T>(HttpRequestData req, T data, string message)
        {
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(ApiResponse<T>.SuccessResponse(data, message));
            return response;
        }

        private async Task<HttpResponseData> CreateErrorResponse(HttpRequestData req, HttpStatusCode statusCode, string message)
        {
            var response = req.CreateResponse(statusCode);
            await response.WriteAsJsonAsync(ApiResponse<object>.ErrorResponse(message));
            return response;
        }
    }

    // Helper classes
    public class OAuthConfig
    {
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string AuthorizationUrl { get; set; } = string.Empty;
        public string TokenUrl { get; set; } = string.Empty;
        public string[] Scopes { get; set; } = Array.Empty<string>();
    }

    public class TokenResponse
    {
        [JsonProperty("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonProperty("refresh_token")]
        public string RefreshToken { get; set; } = string.Empty;

        [JsonProperty("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonProperty("token_type")]
        public string TokenType { get; set; } = string.Empty;
    }
}
