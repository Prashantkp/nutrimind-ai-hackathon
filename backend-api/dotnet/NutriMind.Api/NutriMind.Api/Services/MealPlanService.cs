using Microsoft.EntityFrameworkCore;
using NutriMind.Api.Data;
using NutriMind.Api.Models;
using System.Threading.Tasks;

namespace NutriMind.Api.Services
{
    public interface IMealPlanService
    {
        Task<MealPlan> GetMealPlanAsync(string mealPlanId);
        Task<MealPlan?> GetMealPlanAsync(string userId, string mealPlanId);
        Task<List<MealPlan>> GetUserMealPlansAsync(string userId);
        Task<MealPlan> GetCurrentMealPlanAsync(string userId);
        Task<MealPlan> CreateMealPlanAsync(MealPlan mealPlan);
        Task<MealPlan> UpdateMealPlanAsync(MealPlan mealPlan);
        Task DeleteMealPlanAsync(string mealPlanId);
        Task<List<MealPlan>> GetMealPlansByWeekAsync(string userId, string weekIdentifier);
        Task<List<MealPlan>> GetMealPlansForWeekAsync(string userId, string weekIdentifier);
    }

    public class MealPlanService : IMealPlanService
    {
        private readonly NutriMindDbContext _context;

        public MealPlanService(NutriMindDbContext context)
        {
            _context = context;
        }

        public async Task<MealPlan> GetMealPlanAsync(string mealPlanId)
        {
            return await _context.MealPlans
                .FirstOrDefaultAsync(mealPlan => mealPlan.Id == mealPlanId);
        }

        public async Task<MealPlan?> GetMealPlanAsync(string userId, string mealPlanId)
        {
            return await _context.MealPlans
                .FirstOrDefaultAsync(mealPlan => mealPlan.Id == mealPlanId && mealPlan.UserId == userId);
        }

        public async Task<List<MealPlan>> GetUserMealPlansAsync(string userId)
        {
            return await _context.MealPlans
                .Where(mealPlan => mealPlan.UserId == userId)
                .OrderByDescending(mealPlan => mealPlan.CreatedAt)
                .ToListAsync();
        }

        public async Task<MealPlan> GetCurrentMealPlanAsync(string userId)
        {
            var currentWeek = GetCurrentWeekIdentifier();
            return await _context.MealPlans
                .Where(mealPlan => mealPlan.UserId == userId && mealPlan.WeekIdentifier == currentWeek)
                .OrderByDescending(mealPlan => mealPlan.CreatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<MealPlan> CreateMealPlanAsync(MealPlan mealPlan)
        {
            mealPlan.Id = Guid.NewGuid().ToString();
            mealPlan.CreatedAt = DateTime.UtcNow;
            mealPlan.UpdatedAt = DateTime.UtcNow;

            if (string.IsNullOrEmpty(mealPlan.WeekIdentifier))
            {
                mealPlan.WeekIdentifier = GetCurrentWeekIdentifier();
            }

            _context.MealPlans.Add(mealPlan);
            await _context.SaveChangesAsync();

            return mealPlan;
        }

        public async Task<MealPlan> UpdateMealPlanAsync(MealPlan mealPlan)
        {
            var existingPlan = await _context.MealPlans
                .FirstOrDefaultAsync(plan => plan.Id == mealPlan.Id);

            if (existingPlan == null)
                throw new InvalidOperationException("Meal plan not found");

            // Update properties
            existingPlan.WeekIdentifier = mealPlan.WeekIdentifier;
            existingPlan.Status = mealPlan.Status;
            existingPlan.DailyMeals = mealPlan.DailyMeals;
            existingPlan.WeeklyNutritionSummary = mealPlan.WeeklyNutritionSummary;
            existingPlan.TotalEstimatedCost = mealPlan.TotalEstimatedCost;
            existingPlan.Preferences = mealPlan.Preferences;
            existingPlan.GenerationMetadata = mealPlan.GenerationMetadata;
            existingPlan.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return existingPlan;
        }

        public async Task DeleteMealPlanAsync(string mealPlanId)
        {
            var mealPlan = await _context.MealPlans
                .FirstOrDefaultAsync(plan => plan.Id == mealPlanId);

            if (mealPlan != null)
            {
                _context.MealPlans.Remove(mealPlan);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<MealPlan>> GetMealPlansByWeekAsync(string userId, string weekIdentifier)
        {
            return await _context.MealPlans
                .Where(mealPlan => mealPlan.UserId == userId && mealPlan.WeekIdentifier == weekIdentifier)
                .OrderByDescending(mealPlan => mealPlan.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<MealPlan>> GetMealPlansForWeekAsync(string userId, string weekIdentifier)
        {
            return await GetMealPlansByWeekAsync(userId, weekIdentifier);
        }

        private string GetCurrentWeekIdentifier()
        {
            var today = DateTime.UtcNow.Date;
            var startOfWeek = today.AddDays(-(int)today.DayOfWeek);
            return startOfWeek.ToString("yyyy-MM-dd");
        }
    }
}
