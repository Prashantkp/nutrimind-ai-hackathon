using Microsoft.EntityFrameworkCore;
using NutriMind.Api.Models;
using Newtonsoft.Json;

namespace NutriMind.Api.Data
{
    public class NutriMindDbContext : DbContext
    {
        public NutriMindDbContext(DbContextOptions<NutriMindDbContext> options) : base(options)
        {
        }

        public DbSet<UserProfile> UserProfiles { get; set; }
        public DbSet<MealPlan> MealPlans { get; set; }
        public DbSet<Recipe> Recipes { get; set; }
        public DbSet<GroceryList> GroceryLists { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // UserProfile configuration
            modelBuilder.Entity<UserProfile>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasMaxLength(100);
                entity.Property(e => e.UserId).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Email).HasMaxLength(255).IsRequired();
                entity.Property(e => e.FirstName).HasMaxLength(100);
                entity.Property(e => e.LastName).HasMaxLength(100);
                entity.Property(e => e.DietaryPreference).HasMaxLength(50);
                entity.Property(e => e.ActivityLevel).HasMaxLength(50);
                entity.Property(e => e.CookingSkillLevel).HasMaxLength(50);
                entity.Property(e => e.ShoppingPreference).HasMaxLength(50);
                entity.Property(e => e.PhoneNumber).HasMaxLength(20);
                
                // JSON columns for complex types
                entity.Property(e => e.Allergens)
                    .HasConversion(
                        v => string.Join(';', v),
                        v => v.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList());
                        
                entity.Property(e => e.Dislikes)
                    .HasConversion(
                        v => string.Join(';', v),
                        v => v.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList());
                        
                entity.Property(e => e.HealthGoals)
                    .HasConversion(
                        v => string.Join(';', v),
                        v => v.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList());
                        
                entity.Property(e => e.PreferredCuisines)
                    .HasConversion(
                        v => string.Join(';', v),
                        v => v.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList());
                        
                entity.Property(e => e.KitchenEquipment)
                    .HasConversion(
                        v => string.Join(';', v),
                        v => v.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList());

                // Store complex objects as JSON
                entity.OwnsOne(e => e.NotificationPreferences, np =>
                {
                    np.Property(p => p.EmailEnabled);
                    np.Property(p => p.PushEnabled);
                    np.Property(p => p.MealReminders);
                    np.Property(p => p.GroceryReminders);
                    np.Property(p => p.WeeklyPlanReminders);
                    np.Property(p => p.NutritionTips);
                    np.Property(p => p.ReminderTimes)
                        .HasConversion(
                            v => string.Join(';', v),
                            v => v.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList());
                });

                entity.OwnsOne(e => e.ConnectedServices, cs =>
                {
                    cs.OwnsOne(s => s.Instacart);
                    cs.OwnsOne(s => s.AmazonFresh);
                    cs.OwnsOne(s => s.WalmartGrocery);
                    cs.OwnsOne(s => s.Kroger);
                });

                // Store UserPreferences as JSON to avoid complex nested owned entity issues
                entity.Property(e => e.Preferences)
                    .HasConversion(
                        v => v == null ? null : JsonConvert.SerializeObject(v),
                        v => string.IsNullOrEmpty(v) ? null : JsonConvert.DeserializeObject<UserPreferences>(v));

                entity.HasIndex(e => e.UserId).IsUnique();
                entity.HasIndex(e => e.Email).IsUnique();
            });

            // MealPlan configuration
            modelBuilder.Entity<MealPlan>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasMaxLength(100);
                entity.Property(e => e.UserId).HasMaxLength(100).IsRequired();
                entity.Property(e => e.WeekIdentifier).HasMaxLength(20).IsRequired();
                entity.Property(e => e.Status).HasConversion<string>();

                // Store complex objects as JSON strings
                entity.Property(e => e.DailyMeals)
                    .HasConversion(
                        v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions)null),
                        v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, DailyMealPlan>>(v, (System.Text.Json.JsonSerializerOptions)null) ?? new Dictionary<string, DailyMealPlan>());

                entity.OwnsOne(e => e.WeeklyNutritionSummary);
                entity.OwnsOne(e => e.Preferences);
                entity.OwnsOne(e => e.GenerationMetadata);

                entity.HasIndex(e => new { e.UserId, e.WeekIdentifier }).IsUnique();
            });

            // Recipe configuration
            modelBuilder.Entity<Recipe>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasMaxLength(100);
                entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
                entity.Property(e => e.Cuisine).HasMaxLength(50);
                entity.Property(e => e.Difficulty).HasMaxLength(20);
                entity.Property(e => e.Source).HasMaxLength(500);

                // Store arrays as JSON strings
                entity.Property(e => e.Steps)
                    .HasConversion(
                        v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions)null),
                        v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions)null) ?? new List<string>());

                entity.Property(e => e.Ingredients)
                    .HasConversion(
                        v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions)null),
                        v => System.Text.Json.JsonSerializer.Deserialize<List<Ingredient>>(v, (System.Text.Json.JsonSerializerOptions)null) ?? new List<Ingredient>());

                entity.Property(e => e.Tags)
                    .HasConversion(
                        v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions)null),
                        v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions)null) ?? new List<string>());
            });

            // GroceryList configuration
            modelBuilder.Entity<GroceryList>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasMaxLength(100);

                entity.Property(e => e.Items)
                    .HasConversion(
                        v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions)null),
                        v => System.Text.Json.JsonSerializer.Deserialize<List<GroceryItem>>(v, (System.Text.Json.JsonSerializerOptions)null) ?? new List<GroceryItem>());

                entity.Property(e => e.CategorizedItems)
                    .HasConversion(
                        v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions)null),
                        v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, List<GroceryItem>>>(v, (System.Text.Json.JsonSerializerOptions)null) ?? new Dictionary<string, List<GroceryItem>>());
            });
        }
    }
}
