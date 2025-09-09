using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using NutriMind.Api.Models;
using NutriMind.Api.Services;

namespace NutriMind.Api.Activities
{
    public class LoadUserProfileActivity
    {
        private readonly ILogger<LoadUserProfileActivity> _logger;
        private readonly IUserProfileService _userProfileService;

        public LoadUserProfileActivity(ILogger<LoadUserProfileActivity> logger, IUserProfileService userProfileService)
        {
            _logger = logger;
            _userProfileService = userProfileService;
        }

        [Function("LoadUserProfile")]
        public async Task<UserProfile?> Run([ActivityTrigger] string userId)
        {
            try
            {
                _logger.LogInformation("Loading user profile for user: {UserId}", userId);

                var userProfile = await _userProfileService.GetUserProfileAsync(userId);

                if (userProfile == null)
                {
                    _logger.LogWarning("User profile not found for user: {UserId}", userId);
                    return null;
                }

                _logger.LogInformation("Successfully loaded user profile for: {UserId}", userId);
                return userProfile;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user profile for user: {UserId}", userId);
                throw;
            }
        }
    }
}
