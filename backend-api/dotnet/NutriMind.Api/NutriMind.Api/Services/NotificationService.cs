using NutriMind.Api.Models;
using System.Threading.Tasks;

namespace NutriMind.Api.Services
{
    public interface INotificationService
    {
        Task<NotificationResult> SendMealReminderAsync(UserProfile userProfile, MealReminderRequest request);
        Task<NotificationResult> SendGroceryReminderAsync(UserProfile userProfile, GroceryReminderRequest request);
        Task<NotificationResult> SendWeeklyPlanNotificationAsync(UserProfile userProfile, MealPlan mealPlan);
        Task<NotificationResult> SendNutritionTipAsync(UserProfile userProfile, string tip);
        Task<NotificationSettings> GetNotificationSettingsAsync(string userId);
        Task<NotificationSettings> UpdateNotificationSettingsAsync(string userId, NotificationSettings settings);
        Task<List<ReminderSchedule>> ScheduleRemindersAsync(UserProfile userProfile, MealPlan mealPlan);
    }

    public class NotificationService : INotificationService
    {
        public async Task<NotificationResult> SendMealReminderAsync(UserProfile userProfile, MealReminderRequest request)
        {
            // Mock implementation for meal reminders
            await Task.Delay(100);

            var subject = "NutriMind - Meal Reminder";
            var message = request.CustomMessage ?? 
                $"Time for {request.MealType}! Your meal: {request.RecipeName ?? "your planned meal"}";

            // Simulate sending based on user preferences
            if (userProfile.NotificationPreferences?.EmailEnabled == true && 
                userProfile.NotificationPreferences?.MealReminders == true)
            {
                return new NotificationResult
                {
                    Type = "email",
                    Success = true,
                    Recipient = userProfile.Email,
                    Subject = subject,
                    Message = message,
                    SentAt = DateTime.UtcNow
                };
            }

            if (userProfile.NotificationPreferences?.PushEnabled == true && 
                userProfile.NotificationPreferences?.MealReminders == true)
            {
                return new NotificationResult
                {
                    Type = "push",
                    Success = true,
                    Recipient = userProfile.PhoneNumber ?? userProfile.Email,
                    Subject = subject,
                    Message = message,
                    SentAt = DateTime.UtcNow
                };
            }

            return new NotificationResult
            {
                Type = "meal_reminder",
                Success = false,
                ErrorMessage = "No notification methods enabled for meal reminders",
                SentAt = DateTime.UtcNow
            };
        }

        public async Task<NotificationResult> SendGroceryReminderAsync(UserProfile userProfile, GroceryReminderRequest request)
        {
            await Task.Delay(100);

            var subject = "NutriMind - Grocery Shopping Reminder";
            var message = request.CustomMessage ?? 
                "Don't forget to shop for your meal plan ingredients!";

            if (!string.IsNullOrEmpty(request.SuggestedStore))
            {
                message += $" We suggest shopping at: {request.SuggestedStore}";
            }

            if (userProfile.NotificationPreferences?.EmailEnabled == true && 
                userProfile.NotificationPreferences?.GroceryReminders == true)
            {
                return new NotificationResult
                {
                    Type = "email",
                    Success = true,
                    Recipient = userProfile.Email,
                    Subject = subject,
                    Message = message,
                    SentAt = DateTime.UtcNow
                };
            }

            return new NotificationResult
            {
                Type = "grocery_reminder",
                Success = false,
                ErrorMessage = "No notification methods enabled for grocery reminders",
                SentAt = DateTime.UtcNow
            };
        }

        public async Task<NotificationResult> SendWeeklyPlanNotificationAsync(UserProfile userProfile, MealPlan mealPlan)
        {
            await Task.Delay(100);

            var subject = "NutriMind - Your Weekly Meal Plan is Ready!";
            var mealCount = mealPlan.DailyMeals?.Values.Sum(d => 
                d.Meals.Count + (d.Snacks?.Count ?? 0)) ?? 0;

            var message = $"Your personalized meal plan for the week of {mealPlan.WeekStartDate:MMM dd} is ready! " +
                         $"It includes {mealCount} delicious meals and costs approximately ${mealPlan.TotalEstimatedCost:F2}.";

            if (userProfile.NotificationPreferences?.EmailEnabled == true && 
                userProfile.NotificationPreferences?.WeeklyPlanReminders == true)
            {
                return new NotificationResult
                {
                    Type = "email",
                    Success = true,
                    Recipient = userProfile.Email,
                    Subject = subject,
                    Message = message,
                    SentAt = DateTime.UtcNow
                };
            }

            return new NotificationResult
            {
                Type = "weekly_plan",
                Success = false,
                ErrorMessage = "Weekly plan notifications not enabled",
                SentAt = DateTime.UtcNow
            };
        }

        public async Task<NotificationResult> SendNutritionTipAsync(UserProfile userProfile, string tip)
        {
            await Task.Delay(100);

            var subject = "NutriMind - Daily Nutrition Tip";
            var message = $"Here's your personalized nutrition tip: {tip}";

            if (userProfile.NotificationPreferences?.EmailEnabled == true && 
                userProfile.NotificationPreferences?.NutritionTips == true)
            {
                return new NotificationResult
                {
                    Type = "email",
                    Success = true,
                    Recipient = userProfile.Email,
                    Subject = subject,
                    Message = message,
                    SentAt = DateTime.UtcNow
                };
            }

            return new NotificationResult
            {
                Type = "nutrition_tip",
                Success = false,
                ErrorMessage = "Nutrition tips not enabled",
                SentAt = DateTime.UtcNow
            };
        }

        public async Task<NotificationSettings> GetNotificationSettingsAsync(string userId)
        {
            // In a real implementation, this would fetch from database
            await Task.Delay(50);

            return new NotificationSettings
            {
                EmailNotifications = true,
                PushNotifications = true,
                MealReminders = true,
                GroceryReminders = true,
                WeeklyPlanReminders = true,
                NutritionTips = false,
                ReminderTimes = new List<string> { "09:00", "12:00", "18:00" }
            };
        }

        public async Task<NotificationSettings> UpdateNotificationSettingsAsync(string userId, NotificationSettings settings)
        {
            // In a real implementation, this would update the database
            await Task.Delay(50);
            
            return settings;
        }

        public async Task<List<ReminderSchedule>> ScheduleRemindersAsync(UserProfile userProfile, MealPlan mealPlan)
        {
            await Task.Delay(100);

            var reminders = new List<ReminderSchedule>();
            var startDate = mealPlan.WeekStartDate;
            var reminderTimes = userProfile.NotificationPreferences?.ReminderTimes ?? 
                               new List<string> { "09:00", "12:00", "18:00" };

            if (mealPlan.DailyMeals != null)
            {
                foreach (var kvp in mealPlan.DailyMeals)
                {
                    if (DateTime.TryParse(kvp.Key, out var mealDate))
                    {
                        var dailyMeal = kvp.Value;

                        // Create reminders for each meal
                        int timeIndex = 0;
                        foreach (var mealKvp in dailyMeal.Meals)
                        {
                            if (timeIndex < reminderTimes.Count)
                            {
                                var mealTime = TimeSpan.Parse(reminderTimes[timeIndex]);
                                reminders.Add(new ReminderSchedule
                                {
                                    Id = Guid.NewGuid().ToString(),
                                    UserId = userProfile.UserId,
                                    MealPlanId = mealPlan.Id,
                                    Type = "meal_reminder",
                                    MealType = mealKvp.Key,
                                    RecipeName = mealKvp.Value.Recipe.Name,
                                    ScheduledTime = mealDate.Date.Add(mealTime),
                                    IsActive = true,
                                    CreatedAt = DateTime.UtcNow
                                });
                                timeIndex++;
                            }
                        }
                    }
                }
            }

            // Add grocery reminder for start of week
            if (userProfile.NotificationPreferences?.GroceryReminders == true)
            {
                reminders.Add(new ReminderSchedule
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = userProfile.UserId,
                    MealPlanId = mealPlan.Id,
                    Type = "grocery_reminder",
                    ScheduledTime = startDate.AddDays(-1).Date.AddHours(10), // Day before week starts
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    Message = "Don't forget to buy groceries for your upcoming meal plan!"
                });
            }

            return reminders;
        }
    }
}
