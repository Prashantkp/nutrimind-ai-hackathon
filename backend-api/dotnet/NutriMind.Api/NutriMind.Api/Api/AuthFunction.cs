using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using NutriMind.Api.Models.DTOs;
using NutriMind.Api.Services;
using System.Net;
using System.Text.Json;

namespace NutriMind.Api.Api
{
    public class AuthFunction
    {
        private readonly ILogger<AuthFunction> _logger;
        private readonly IUserService _userService;

        public AuthFunction(ILogger<AuthFunction> logger, IUserService userService)
        {
            _logger = logger;
            _userService = userService;
        }

        [Function("Register")]
        public async Task<HttpResponseData> Register([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "auth/register")] HttpRequestData req)
        {
            _logger.LogInformation("Processing user registration request");

            try
            {
                var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var registerRequest = JsonSerializer.Deserialize<RegisterRequest>(requestBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (registerRequest == null)
                {
                    return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Invalid request body");
                }

                // Basic validation
                if (string.IsNullOrEmpty(registerRequest.Email) || 
                    string.IsNullOrEmpty(registerRequest.Password) ||
                    string.IsNullOrEmpty(registerRequest.FirstName) ||
                    string.IsNullOrEmpty(registerRequest.LastName))
                {
                    return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "All required fields must be provided");
                }

                if (registerRequest.Password.Length < 8)
                {
                    return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Password must be at least 8 characters long");
                }

                var result = await _userService.RegisterAsync(registerRequest);

                if (!result.Success)
                {
                    return await CreateResponse(req, HttpStatusCode.BadRequest, result);
                }

                return await CreateResponse(req, HttpStatusCode.Created, result);
            }
            catch (JsonException)
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Invalid JSON format");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during user registration");
                return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "Internal server error");
            }
        }

        [Function("Login")]
        public async Task<HttpResponseData> Login([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "auth/login")] HttpRequestData req)
        {
            _logger.LogInformation("Processing user login request");

            try
            {
                var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var loginRequest = JsonSerializer.Deserialize<LoginRequest>(requestBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (loginRequest == null)
                {
                    return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Invalid request body");
                }

                // Basic validation
                if (string.IsNullOrEmpty(loginRequest.Email) || string.IsNullOrEmpty(loginRequest.Password))
                {
                    return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Email and password are required");
                }

                var result = await _userService.LoginAsync(loginRequest);

                if (!result.Success)
                {
                    return await CreateResponse(req, HttpStatusCode.Unauthorized, result);
                }

                return await CreateResponse(req, HttpStatusCode.OK, result);
            }
            catch (JsonException)
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Invalid JSON format");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during user login");
                return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "Internal server error");
            }
        }

        [Function("RefreshToken")]
        public async Task<HttpResponseData> RefreshToken([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "auth/refresh")] HttpRequestData req)
        {
            _logger.LogInformation("Processing token refresh request");

            try
            {
                var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var refreshRequest = JsonSerializer.Deserialize<RefreshTokenRequest>(requestBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (refreshRequest == null)
                {
                    return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Invalid request body");
                }

                if (string.IsNullOrEmpty(refreshRequest.RefreshToken))
                {
                    return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Refresh token is required");
                }

                var result = await _userService.RefreshTokenAsync(refreshRequest);

                if (!result.Success)
                {
                    return await CreateResponse(req, HttpStatusCode.Unauthorized, result);
                }

                return await CreateResponse(req, HttpStatusCode.OK, result);
            }
            catch (JsonException)
            {
                return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Invalid JSON format");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during token refresh");
                return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "Internal server error");
            }
        }

        [Function("GetCurrentUser")]
        public async Task<HttpResponseData> GetCurrentUser([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "auth/me")] HttpRequestData req)
        {
            _logger.LogInformation("Processing get current user request");

            try
            {
                var authHeader = req.Headers.FirstOrDefault(h => h.Key.ToLower() == "authorization").Value?.FirstOrDefault();
                
                if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    return await CreateErrorResponse(req, HttpStatusCode.Unauthorized, "Authorization header is missing or invalid");
                }

                var token = authHeader["Bearer ".Length..].Trim();
                
                // You'll need to implement token validation and user ID extraction in AuthService
                var authService = req.FunctionContext.InstanceServices.GetService(typeof(IAuthService)) as IAuthService;
                var userId = authService?.GetUserIdFromRequest(req);

                if (string.IsNullOrEmpty(userId))
                {
                    return await CreateErrorResponse(req, HttpStatusCode.Unauthorized, "Invalid or expired token");
                }

                var result = await _userService.GetUserByIdAsync(userId);

                if (!result.Success)
                {
                    return await CreateResponse(req, HttpStatusCode.NotFound, result);
                }

                return await CreateResponse(req, HttpStatusCode.OK, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting current user");
                return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, "Internal server error");
            }
        }

        private async Task<HttpResponseData> CreateResponse<T>(HttpRequestData req, HttpStatusCode statusCode, T data)
        {
            var response = req.CreateResponse(statusCode);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");
            
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            
            await response.WriteStringAsync(JsonSerializer.Serialize(data, jsonOptions));
            return response;
        }

        private async Task<HttpResponseData> CreateErrorResponse(HttpRequestData req, HttpStatusCode statusCode, string message)
        {
            var errorResponse = new Models.ApiResponse<object>
            {
                Success = false,
                Message = message,
                Data = null,
                Errors = new List<string> { message }
            };

            return await CreateResponse(req, statusCode, errorResponse);
        }
    }
}
