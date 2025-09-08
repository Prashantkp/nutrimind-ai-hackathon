using Microsoft.EntityFrameworkCore;
using NutriMind.Api.Data;
using NutriMind.Api.Models;
using System.Threading.Tasks;

namespace NutriMind.Api.Services
{
    public interface IUserProfileService
    {
        Task<UserProfile> GetUserProfileAsync(string userId);
        Task<UserProfile> CreateUserProfileAsync(UserProfile userProfile);
        Task<UserProfile> UpdateUserProfileAsync(UserProfile userProfile);
        Task DeleteUserProfileAsync(string userId);
    }

    public class UserProfileService : IUserProfileService
    {
        private readonly NutriMindDbContext _context;

        public UserProfileService(NutriMindDbContext context)
        {
            _context = context;
        }

        public async Task<UserProfile> GetUserProfileAsync(string userId)
        {
            return await _context.UserProfiles
                .FirstOrDefaultAsync(u => u.UserId == userId);
        }

        public async Task<UserProfile> CreateUserProfileAsync(UserProfile userProfile)
        {
            userProfile.Id = Guid.NewGuid().ToString();
            userProfile.CreatedAt = DateTime.UtcNow;
            userProfile.UpdatedAt = DateTime.UtcNow;

            _context.UserProfiles.Add(userProfile);
            await _context.SaveChangesAsync();

            return userProfile;
        }

        public async Task<UserProfile> UpdateUserProfileAsync(UserProfile userProfile)
        {
            var existingProfile = await _context.UserProfiles
                .FirstOrDefaultAsync(u => u.UserId == userProfile.UserId);

            if (existingProfile == null)
                throw new InvalidOperationException("User profile not found");

            // Update properties
            existingProfile.Email = userProfile.Email;
            existingProfile.FirstName = userProfile.FirstName;
            existingProfile.LastName = userProfile.LastName;
            existingProfile.Age = userProfile.Age;
            existingProfile.Height = userProfile.Height;
            existingProfile.Weight = userProfile.Weight;
            existingProfile.ActivityLevel = userProfile.ActivityLevel;
            existingProfile.DietaryPreference = userProfile.DietaryPreference;
            existingProfile.Allergens = userProfile.Allergens;
            existingProfile.Dislikes = userProfile.Dislikes;
            existingProfile.HealthGoals = userProfile.HealthGoals;
            existingProfile.PreferredCuisines = userProfile.PreferredCuisines;
            existingProfile.CookingSkillLevel = userProfile.CookingSkillLevel;
            existingProfile.KitchenEquipment = userProfile.KitchenEquipment;
            existingProfile.ShoppingPreference = userProfile.ShoppingPreference;
            existingProfile.NotificationPreferences = userProfile.NotificationPreferences;
            existingProfile.ConnectedServices = userProfile.ConnectedServices;
            existingProfile.PhoneNumber = userProfile.PhoneNumber;
            existingProfile.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return existingProfile;
        }

        public async Task DeleteUserProfileAsync(string userId)
        {
            var userProfile = await _context.UserProfiles
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (userProfile != null)
            {
                _context.UserProfiles.Remove(userProfile);
                await _context.SaveChangesAsync();
            }
        }
    }
}
