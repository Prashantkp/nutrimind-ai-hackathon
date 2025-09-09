using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NutriMind.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GroceryLists",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Items = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CategorizedItems = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TotalEstimatedCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroceryLists", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Recipes",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Cuisine = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Steps = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Ingredients = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsVegan = table.Column<bool>(type: "bit", nullable: false),
                    IsKeto = table.Column<bool>(type: "bit", nullable: false),
                    IsDiabeticFriendly = table.Column<bool>(type: "bit", nullable: false),
                    Calories = table.Column<int>(type: "int", nullable: false),
                    Tags = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Difficulty = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TotalTime = table.Column<int>(type: "int", nullable: false),
                    Servings = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Recipes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserProfiles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Age = table.Column<int>(type: "int", nullable: false),
                    Height = table.Column<float>(type: "real", nullable: false),
                    Weight = table.Column<float>(type: "real", nullable: false),
                    ActivityLevel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DietaryPreference = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Allergens = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Dislikes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HealthGoals = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TargetCalories = table.Column<int>(type: "int", nullable: false),
                    TargetProtein = table.Column<int>(type: "int", nullable: false),
                    TargetCarbs = table.Column<int>(type: "int", nullable: false),
                    TargetFats = table.Column<int>(type: "int", nullable: false),
                    MealFrequency = table.Column<int>(type: "int", nullable: false),
                    SnackFrequency = table.Column<int>(type: "int", nullable: false),
                    CookingSkillLevel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CookingTimePreference = table.Column<int>(type: "int", nullable: false),
                    BudgetPerWeek = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PreferredCuisines = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    KitchenEquipment = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ShoppingPreference = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NotificationPreferences_EmailEnabled = table.Column<bool>(type: "bit", nullable: false),
                    NotificationPreferences_PushEnabled = table.Column<bool>(type: "bit", nullable: false),
                    NotificationPreferences_MealReminders = table.Column<bool>(type: "bit", nullable: false),
                    NotificationPreferences_GroceryReminders = table.Column<bool>(type: "bit", nullable: false),
                    NotificationPreferences_WeeklyPlanReminders = table.Column<bool>(type: "bit", nullable: false),
                    NotificationPreferences_NutritionTips = table.Column<bool>(type: "bit", nullable: false),
                    NotificationPreferences_ReminderTimes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NotificationPreferences_ShoppingReminders = table.Column<bool>(type: "bit", nullable: false),
                    NotificationPreferences_WeeklyPlanGeneration = table.Column<bool>(type: "bit", nullable: false),
                    NotificationPreferences_NutritionInsights = table.Column<bool>(type: "bit", nullable: false),
                    NotificationPreferences_PreferredReminderTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    Preferences = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConnectedServices_Instacart_IsConnected = table.Column<bool>(type: "bit", nullable: true),
                    ConnectedServices_Instacart_AccessToken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConnectedServices_Instacart_RefreshToken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConnectedServices_Instacart_ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ConnectedServices_Instacart_ConnectedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ConnectedServices_AmazonFresh_IsConnected = table.Column<bool>(type: "bit", nullable: true),
                    ConnectedServices_AmazonFresh_AccessToken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConnectedServices_AmazonFresh_RefreshToken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConnectedServices_AmazonFresh_ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ConnectedServices_AmazonFresh_ConnectedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ConnectedServices_WalmartGrocery_IsConnected = table.Column<bool>(type: "bit", nullable: true),
                    ConnectedServices_WalmartGrocery_AccessToken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConnectedServices_WalmartGrocery_RefreshToken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConnectedServices_WalmartGrocery_ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ConnectedServices_WalmartGrocery_ConnectedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ConnectedServices_Kroger_IsConnected = table.Column<bool>(type: "bit", nullable: true),
                    ConnectedServices_Kroger_AccessToken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConnectedServices_Kroger_RefreshToken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConnectedServices_Kroger_ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ConnectedServices_Kroger_ConnectedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MealPlans",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    WeekOf = table.Column<DateTime>(type: "datetime2", nullable: false),
                    WeekIdentifier = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DailyMeals = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    WeeklyNutritionSummary_TotalCalories = table.Column<int>(type: "int", nullable: false),
                    WeeklyNutritionSummary_TotalProtein = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    WeeklyNutritionSummary_TotalCarbs = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    WeeklyNutritionSummary_TotalFats = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    WeeklyNutritionSummary_AverageDailyCalories = table.Column<int>(type: "int", nullable: false),
                    WeeklyNutritionSummary_TargetCalories = table.Column<int>(type: "int", nullable: false),
                    WeeklyNutritionSummary_AdherencePercentage = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GroceryListId = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    TotalEstimatedCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Preferences_DietaryPreference = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Preferences_Allergens = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Preferences_Dislikes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Preferences_TargetCalories = table.Column<int>(type: "int", nullable: false),
                    Preferences_MaxPrepTime = table.Column<int>(type: "int", nullable: false),
                    Preferences_BudgetConstraint = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Preferences_VarietyLevel = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GenerationMetadata_StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GenerationMetadata_CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    GenerationMetadata_OrchestrationId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GenerationMetadata_AiModel = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GenerationMetadata_RecipesConsidered = table.Column<int>(type: "int", nullable: false),
                    GenerationMetadata_GenerationTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    GenerationMetadata_Errors = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MealPlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MealPlans_GroceryLists_GroceryListId",
                        column: x => x.GroceryListId,
                        principalTable: "GroceryLists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MealPlans_GroceryListId",
                table: "MealPlans",
                column: "GroceryListId");

            migrationBuilder.CreateIndex(
                name: "IX_MealPlans_UserId_WeekIdentifier",
                table: "MealPlans",
                columns: new[] { "UserId", "WeekIdentifier" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_Email",
                table: "UserProfiles",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_UserId",
                table: "UserProfiles",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MealPlans");

            migrationBuilder.DropTable(
                name: "Recipes");

            migrationBuilder.DropTable(
                name: "UserProfiles");

            migrationBuilder.DropTable(
                name: "GroceryLists");
        }
    }
}
