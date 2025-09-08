using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using NutriMind.Api.Models;

namespace NutriMind.Api.Activities
{
    public class ScheduleRemindersActivity
    {
        private readonly ILogger<ScheduleRemindersActivity> _logger;

        public ScheduleRemindersActivity(ILogger<ScheduleRemindersActivity> logger)
        {
            _logger = logger;
        }

        [Function("ScheduleReminders")]
        public async Task<List<ReminderSchedule>> Run([ActivityTrigger] ScheduleRemindersInput input)
        {
            try
            {
                _logger.LogInformation("Scheduling reminders for meal plan: {MealPlanId}", input.MealPlan.Id);

                var scheduledReminders = new List<ReminderSchedule>();

                // Schedule meal preparation reminders
                if (input.UserProfile.NotificationPreferences.MealReminders)
                {
                    var mealReminders = await ScheduleMealReminders(input.MealPlan, input.UserProfile);
                    scheduledReminders.AddRange(mealReminders);
                }

                // Schedule shopping reminders
                if (input.UserProfile.NotificationPreferences.ShoppingReminders)
                {
                    var shoppingReminders = await ScheduleShoppingReminders(input.MealPlan, input.UserProfile);
                    scheduledReminders.AddRange(shoppingReminders);
                }

                // Schedule weekly plan generation reminders
                if (input.UserProfile.NotificationPreferences.WeeklyPlanGeneration)
                {
                    var weeklyReminders = await ScheduleWeeklyPlanReminders(input.MealPlan, input.UserProfile);
                    scheduledReminders.AddRange(weeklyReminders);
                }

                // Schedule nutrition insights
                if (input.UserProfile.NotificationPreferences.NutritionInsights)
                {
                    var nutritionReminders = await ScheduleNutritionInsights(input.MealPlan, input.UserProfile);
                    scheduledReminders.AddRange(nutritionReminders);
                }

                _logger.LogInformation("Scheduled {ReminderCount} reminders for meal plan: {MealPlanId}", 
                    scheduledReminders.Count, input.MealPlan.Id);

                return scheduledReminders;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scheduling reminders for meal plan: {MealPlanId}", input.MealPlan.Id);
                throw;
            }
        }

        private async Task<List<ReminderSchedule>> ScheduleMealReminders(MealPlan mealPlan, UserProfile userProfile)
        {
            var reminders = new List<ReminderSchedule>();
            var preferredTime = userProfile.NotificationPreferences.PreferredReminderTime;

            foreach (var dailyPlan in mealPlan.DailyMeals.Values)
            {
                // Schedule prep reminders for complex meals
                foreach (var meal in dailyPlan.Meals.Values)
                {
                    if (meal.EstimatedPrepTime + meal.EstimatedCookTime > 30) // Only for meals requiring more than 30 minutes
                    {
                        var reminderTime = dailyPlan.Date.Add(preferredTime).AddHours(-2); // 2 hours before preferred time
                        
                        reminders.Add(new ReminderSchedule
                        {
                            Id = Guid.NewGuid().ToString(),
                            UserId = userProfile.UserId,
                            MealPlanId = mealPlan.Id,
                            Type = ReminderType.MealPreparation,
                            ScheduledFor = reminderTime,
                            Title = $"Time to prepare {meal.Name}",
                            Message = $"Don't forget to prepare {meal.Name} for {meal.MealType}. " +
                                     $"Estimated prep time: {meal.EstimatedPrepTime} minutes, " +
                                     $"cook time: {meal.EstimatedCookTime} minutes.",
                            MealId = meal.Id,
                            IsActive = true
                        });
                    }

                    // Schedule meal time reminders
                    if (meal.ScheduledTime.HasValue)
                    {
                        var mealReminderTime = dailyPlan.Date.Add(meal.ScheduledTime.Value).AddMinutes(-15);
                        
                        reminders.Add(new ReminderSchedule
                        {
                            Id = Guid.NewGuid().ToString(),
                            UserId = userProfile.UserId,
                            MealPlanId = mealPlan.Id,
                            Type = ReminderType.MealTime,
                            ScheduledFor = mealReminderTime,
                            Title = $"{meal.MealType.ToUpper()} TIME!",
                            Message = $"It's time for {meal.Name}! 🍽️",
                            MealId = meal.Id,
                            IsActive = true
                        });
                    }
                }
            }

            return reminders;
        }

        private async Task<List<ReminderSchedule>> ScheduleShoppingReminders(MealPlan mealPlan, UserProfile userProfile)
        {
            var reminders = new List<ReminderSchedule>();
            
            // Schedule shopping reminder for the weekend before the meal plan week
            var shoppingDay = mealPlan.WeekOf.AddDays(-2); // Friday before the week
            var shoppingTime = shoppingDay.Add(userProfile.NotificationPreferences.PreferredReminderTime);

            reminders.Add(new ReminderSchedule
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userProfile.UserId,
                MealPlanId = mealPlan.Id,
                Type = ReminderType.Shopping,
                ScheduledFor = shoppingTime,
                Title = "Time to go grocery shopping! 🛒",
                Message = $"Your grocery list is ready with {mealPlan.GroceryList.Items.Count} items. " +
                         $"Estimated cost: ${mealPlan.TotalEstimatedCost:F2}",
                IsActive = true
            });

            return reminders;
        }

        private async Task<List<ReminderSchedule>> ScheduleWeeklyPlanReminders(MealPlan mealPlan, UserProfile userProfile)
        {
            var reminders = new List<ReminderSchedule>();
            
            // Schedule reminder to generate next week's plan
            var nextWeekReminderDate = mealPlan.WeekOf.AddDays(5); // Friday of current week
            var reminderTime = nextWeekReminderDate.Add(userProfile.NotificationPreferences.PreferredReminderTime);

            reminders.Add(new ReminderSchedule
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userProfile.UserId,
                MealPlanId = mealPlan.Id,
                Type = ReminderType.WeeklyPlanGeneration,
                ScheduledFor = reminderTime,
                Title = "Plan next week's meals 📅",
                Message = "Time to create your meal plan for next week. Keep up the healthy eating!",
                IsActive = true
            });

            return reminders;
        }

        private async Task<List<ReminderSchedule>> ScheduleNutritionInsights(MealPlan mealPlan, UserProfile userProfile)
        {
            var reminders = new List<ReminderSchedule>();
            
            // Schedule mid-week nutrition check-in
            var midWeekDate = mealPlan.WeekOf.AddDays(3); // Thursday
            var insightTime = midWeekDate.Add(userProfile.NotificationPreferences.PreferredReminderTime);

            reminders.Add(new ReminderSchedule
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userProfile.UserId,
                MealPlanId = mealPlan.Id,
                Type = ReminderType.NutritionInsight,
                ScheduledFor = insightTime,
                Title = "Weekly nutrition check-in 📊",
                Message = "How's your week going? Check your nutrition progress and meal ratings.",
                IsActive = true
            });

            // Schedule end-of-week summary
            var endWeekDate = mealPlan.WeekOf.AddDays(6); // Sunday
            var summaryTime = endWeekDate.Add(new TimeSpan(20, 0, 0)); // 8 PM

            reminders.Add(new ReminderSchedule
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userProfile.UserId,
                MealPlanId = mealPlan.Id,
                Type = ReminderType.WeeklySummary,
                ScheduledFor = summaryTime,
                Title = "Weekly meal summary 📋",
                Message = "How did this week go? Rate your meals and help us improve next week's plan.",
                IsActive = true
            });

            return reminders;
        }
    }

    public class ScheduleRemindersInput
    {
        public MealPlan MealPlan { get; set; } = new();
        public UserProfile UserProfile { get; set; } = new();
    }

    public class ReminderSchedule
    {
        public string Id { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string MealPlanId { get; set; } = string.Empty;
        public ReminderType Type { get; set; }
        public DateTime ScheduledFor { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? MealId { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsSent { get; set; } = false;
        public DateTime? SentAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public enum ReminderType
    {
        MealPreparation,
        MealTime,
        Shopping,
        WeeklyPlanGeneration,
        NutritionInsight,
        WeeklySummary
    }
}
