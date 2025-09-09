using System;
using System.Collections.Generic;

namespace NutriMind.Api.Models
{
    public class IntegrationStatus
    {
        public string ServiceType { get; set; } = string.Empty;
        public bool IsConnected { get; set; }
        public string? ConnectionId { get; set; }
        public DateTime? ConnectedAt { get; set; }
        public DateTime? DisconnectedAt { get; set; }
        public DateTime? LastAttempt { get; set; }
        public string? ErrorMessage { get; set; }
        public Dictionary<string, object>? AccountInfo { get; set; }
    }

    public class ConnectedServicesStatus
    {
        public IntegrationStatus Instacart { get; set; } = new();
        public IntegrationStatus AmazonFresh { get; set; } = new();
        public IntegrationStatus WalmartGrocery { get; set; } = new();
        public IntegrationStatus Kroger { get; set; } = new();
    }

    public class ReminderSchedule
    {
        public string Id { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string MealPlanId { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // "meal_reminder", "grocery_reminder", "nutrition_tip"
        public string? MealType { get; set; } // "Breakfast", "Lunch", "Dinner"
        public string? RecipeName { get; set; }
        public DateTime ScheduledTime { get; set; }
        public bool IsActive { get; set; }
        public bool IsSent { get; set; }
        public DateTime? SentAt { get; set; }
        public string? Message { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
