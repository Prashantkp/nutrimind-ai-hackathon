using Microsoft.EntityFrameworkCore;
using NutriMind.Api.Data;
using NutriMind.Api.Models;
using NutriMind.Api.Models.DTOs;
using NutriMind.Api.Services;
using BCrypt.Net;

namespace NutriMind.Api.Services
{
    public interface IUserService
    {
        Task<Models.ApiResponse<AuthResponse>> RegisterAsync(RegisterRequest request);
        Task<Models.ApiResponse<AuthResponse>> LoginAsync(LoginRequest request);
        Task<Models.ApiResponse<AuthResponse>> RefreshTokenAsync(RefreshTokenRequest request);
        Task<Models.ApiResponse<UserDto>> GetUserByIdAsync(string userId);
        Task<bool> EmailExistsAsync(string email);
    }

    public class UserService : IUserService
    {
        private readonly NutriMindDbContext _context;
        private readonly IAuthService _authService;

        public UserService(NutriMindDbContext context, IAuthService authService)
        {
            _context = context;
            _authService = authService;
        }

        public async Task<Models.ApiResponse<AuthResponse>> RegisterAsync(RegisterRequest request)
        {
            try
            {
                // Check if email already exists
                if (await EmailExistsAsync(request.Email))
                {
                    return new Models.ApiResponse<AuthResponse>
                    {
                        Success = false,
                        Message = "Email already exists",
                        Errors = new List<string> { "A user with this email address already exists." }
                    };
                }

                // Create new user
                var user = new User
                {
                    Id = Guid.NewGuid().ToString(),
                    Email = request.Email.ToLowerInvariant(),
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    PhoneNumber = request.PhoneNumber,
                    IsEmailVerified = false,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                // Generate refresh token
                var refreshToken = _authService.GenerateRefreshToken();
                user.RefreshToken = refreshToken;
                user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Generate JWT token
                var token = _authService.GenerateToken(user.Id, user.Email);
                var expiresAt = DateTime.UtcNow.AddHours(1);

                var authResponse = new AuthResponse
                {
                    Token = token,
                    RefreshToken = refreshToken,
                    ExpiresAt = expiresAt,
                    User = MapToUserDto(user)
                };

                return new Models.ApiResponse<AuthResponse>
                {
                    Success = true,
                    Message = "User registered successfully",
                    Data = authResponse
                };
            }
            catch (Exception ex)
            {
                return new Models.ApiResponse<AuthResponse>
                {
                    Success = false,
                    Message = "Registration failed",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<Models.ApiResponse<AuthResponse>> LoginAsync(LoginRequest request)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == request.Email.ToLowerInvariant());

                if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                {
                    return new Models.ApiResponse<AuthResponse>
                    {
                        Success = false,
                        Message = "Invalid email or password",
                        Errors = new List<string> { "Invalid login credentials." }
                    };
                }

                if (!user.IsActive)
                {
                    return new Models.ApiResponse<AuthResponse>
                    {
                        Success = false,
                        Message = "Account is deactivated",
                        Errors = new List<string> { "This account has been deactivated." }
                    };
                }

                // Generate refresh token
                var refreshToken = _authService.GenerateRefreshToken();
                user.RefreshToken = refreshToken;
                user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
                user.LastLoginAt = DateTime.UtcNow;
                user.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Generate JWT token
                var token = _authService.GenerateToken(user.Id, user.Email);
                var expiresAt = DateTime.UtcNow.AddHours(1);

                // Check if user has profile
                var hasProfile = await _context.UserProfiles
                    .AnyAsync(p => p.UserId == user.Id);

                var userDto = MapToUserDto(user);
                userDto.HasProfile = hasProfile;

                var authResponse = new AuthResponse
                {
                    Token = token,
                    RefreshToken = refreshToken,
                    ExpiresAt = expiresAt,
                    User = userDto
                };

                return new Models.ApiResponse<AuthResponse>
                {
                    Success = true,
                    Message = "Login successful",
                    Data = authResponse
                };
            }
            catch (Exception ex)
            {
                return new Models.ApiResponse<AuthResponse>
                {
                    Success = false,
                    Message = "Login failed",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<Models.ApiResponse<AuthResponse>> RefreshTokenAsync(RefreshTokenRequest request)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.RefreshToken == request.RefreshToken);

                if (user == null || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
                {
                    return new Models.ApiResponse<AuthResponse>
                    {
                        Success = false,
                        Message = "Invalid or expired refresh token",
                        Errors = new List<string> { "The refresh token is invalid or has expired." }
                    };
                }

                // Generate new tokens
                var newRefreshToken = _authService.GenerateRefreshToken();
                user.RefreshToken = newRefreshToken;
                user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
                user.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                var token = _authService.GenerateToken(user.Id, user.Email);
                var expiresAt = DateTime.UtcNow.AddHours(1);

                // Check if user has profile
                var hasProfile = await _context.UserProfiles
                    .AnyAsync(p => p.UserId == user.Id);

                var userDto = MapToUserDto(user);
                userDto.HasProfile = hasProfile;

                var authResponse = new AuthResponse
                {
                    Token = token,
                    RefreshToken = newRefreshToken,
                    ExpiresAt = expiresAt,
                    User = userDto
                };

                return new Models.ApiResponse<AuthResponse>
                {
                    Success = true,
                    Message = "Token refreshed successfully",
                    Data = authResponse
                };
            }
            catch (Exception ex)
            {
                return new Models.ApiResponse<AuthResponse>
                {
                    Success = false,
                    Message = "Token refresh failed",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<Models.ApiResponse<UserDto>> GetUserByIdAsync(string userId)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                {
                    return new Models.ApiResponse<UserDto>
                    {
                        Success = false,
                        Message = "User not found",
                        Errors = new List<string> { "The specified user does not exist." }
                    };
                }

                // Check if user has profile
                var hasProfile = await _context.UserProfiles
                    .AnyAsync(p => p.UserId == user.Id);

                var userDto = MapToUserDto(user);
                userDto.HasProfile = hasProfile;

                return new Models.ApiResponse<UserDto>
                {
                    Success = true,
                    Message = "User retrieved successfully",
                    Data = userDto
                };
            }
            catch (Exception ex)
            {
                return new Models.ApiResponse<UserDto>
                {
                    Success = false,
                    Message = "Failed to retrieve user",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            return await _context.Users.AnyAsync(u => u.Email == email.ToLowerInvariant());
        }

        private UserDto MapToUserDto(User user)
        {
            return new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                IsEmailVerified = user.IsEmailVerified,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt
            };
        }
    }
}
