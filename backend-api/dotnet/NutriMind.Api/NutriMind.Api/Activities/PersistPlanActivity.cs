using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using NutriMind.Api.Models;
using NutriMind.Api.Services;
using System;
using System.Threading.Tasks;

namespace NutriMind.Api.Activities
{
	public class PersistPlanActivity
	{
		private readonly ILogger<PersistPlanActivity> _logger;
		private readonly IMealPlanService _mealPlanService;

		public PersistPlanActivity(
			ILogger<PersistPlanActivity> logger,
			IMealPlanService mealPlanService)
		{
			_logger = logger;
			_mealPlanService = mealPlanService;
		}

		[Function("PersistPlan")]
		public async Task<MealPlan> Run([ActivityTrigger] MealPlan mealPlan)
		{
			try
			{
				_logger.LogInformation("Persisting meal plan for user: {UserId}, Week: {WeekIdentifier}, Plan ID: {MealPlanId}",
					mealPlan.UserId, mealPlan.WeekIdentifier, mealPlan.Id);

				// Validate required fields before persisting
				if (string.IsNullOrEmpty(mealPlan.UserId))
				{
					throw new ArgumentException("UserId is required for meal plan persistence");
				}

				if (string.IsNullOrEmpty(mealPlan.WeekIdentifier))
				{
					throw new ArgumentException("WeekIdentifier is required for meal plan persistence");
				}

				// Set timestamps if not already set
				if (mealPlan.CreatedAt == default)
				{
					mealPlan.CreatedAt = DateTime.UtcNow;
				}

				mealPlan.UpdatedAt = DateTime.UtcNow;

				// Ensure we have a valid ID
				if (string.IsNullOrEmpty(mealPlan.Id))
				{
					mealPlan.Id = Guid.NewGuid().ToString();
				}

				// Set the status to Generated if not already set
				if (mealPlan.Status == MealPlanStatus.Generating)
				{
					mealPlan.Status = MealPlanStatus.Generated;
				}

				// Check if a meal plan already exists for this user and week
				var existingPlans = await _mealPlanService.GetMealPlansForWeekAsync(mealPlan.UserId, mealPlan.WeekIdentifier);

				MealPlan savedMealPlan;

				if (existingPlans.Any())
				{
					// Update existing meal plan
					var existingPlan = existingPlans.First();

					_logger.LogInformation("Updating existing meal plan: {ExistingPlanId}", existingPlan.Id);

					// Preserve the original ID and creation timestamp
					mealPlan.Id = existingPlan.Id;
					mealPlan.CreatedAt = existingPlan.CreatedAt;

					savedMealPlan = await _mealPlanService.UpdateMealPlanAsync(mealPlan);
				}
				else
				{
					// Create new meal plan
					_logger.LogInformation("Creating new meal plan");
					savedMealPlan = await _mealPlanService.CreateMealPlanAsync(mealPlan);
				}

				_logger.LogInformation("Successfully persisted meal plan: {MealPlanId} for user: {UserId}",
					savedMealPlan.Id, savedMealPlan.UserId);

				// Log some statistics
				var totalMeals = savedMealPlan.DailyMeals.Values.Sum(day => day.Meals.Count);
				var totalCost = savedMealPlan.TotalEstimatedCost;
				var totalCalories = savedMealPlan.WeeklyNutritionSummary?.TotalCalories ?? 0;

				_logger.LogInformation("Meal plan statistics - Total meals: {TotalMeals}, Total cost: ${TotalCost:F2}, Total calories: {TotalCalories}",
					totalMeals, totalCost, totalCalories);

				return savedMealPlan;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error persisting meal plan for user: {UserId}, Week: {WeekIdentifier}",
					mealPlan?.UserId ?? "unknown", mealPlan?.WeekIdentifier ?? "unknown");
				throw;
			}
		}
	}
}