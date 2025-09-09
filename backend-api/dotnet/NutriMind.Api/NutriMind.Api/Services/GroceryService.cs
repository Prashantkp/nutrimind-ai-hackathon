using Microsoft.EntityFrameworkCore;
using NutriMind.Api.Data;
using NutriMind.Api.Models;

namespace NutriMind.Api.Services
{
    public interface IGroceryService
    {
        Task<GroceryList?> GetGroceryListAsync(string mealPlanId);
        Task<GroceryList> UpdateGroceryListAsync(string mealPlanId, GroceryList groceryList);
        Task<GroceryList> GenerateGroceryListFromMealPlanAsync(string mealPlanId);
        Task<List<GroceryItem>> OptimizeShoppingListAsync(List<GroceryItem> items);
        Task<StoreLocationInfo> FindNearbyStoresAsync(string address);
    }

    public class GroceryService : IGroceryService
    {
        private readonly NutriMindDbContext _context;

        public GroceryService(NutriMindDbContext context)
        {
            _context = context;
        }

        public async Task<GroceryList?> GetGroceryListAsync(string mealPlanId)
        {
            var mealPlan = await _context.MealPlans
                .FirstOrDefaultAsync(m => m.Id == mealPlanId);
            return mealPlan?.GroceryList;
        }

        public async Task<GroceryList> UpdateGroceryListAsync(string mealPlanId, GroceryList groceryList)
        {
            var mealPlan = await _context.MealPlans
                .FirstOrDefaultAsync(m => m.Id == mealPlanId);

            if (mealPlan == null)
                throw new InvalidOperationException("Meal plan not found");

            // Calculate total estimated cost
            groceryList.TotalEstimatedCost = groceryList.Items?.Sum(i => i.EstimatedCost) ?? 0;

            mealPlan.GroceryList = groceryList;
            await _context.SaveChangesAsync();

            return groceryList;
        }

        public async Task<GroceryList> GenerateGroceryListFromMealPlanAsync(string mealPlanId)
        {
            var mealPlan = await _context.MealPlans
                .FirstOrDefaultAsync(m => m.Id == mealPlanId);

            if (mealPlan == null)
                throw new InvalidOperationException("Meal plan not found");

            var groceryItems = new List<GroceryItem>();
            
            // Extract ingredients from all meals in the week
            foreach (var dailyMeal in mealPlan.DailyMeals.Values)
            {
                foreach (var meal in dailyMeal.Meals.Values)
                {
                    foreach (var ingredient in meal.Recipe.Ingredients)
                    {
                        var existingItem = groceryItems.FirstOrDefault(gi => 
                            gi.Name.Equals(ingredient.Item, StringComparison.OrdinalIgnoreCase));

                        if (existingItem != null)
                        {
                            // Combine quantities - simplified approach
                            existingItem.Quantity = $"{existingItem.Quantity} + {ingredient.Qty} {ingredient.Unit}";
                        }
                        else
                        {
                            groceryItems.Add(new GroceryItem
                            {
                                Name = ingredient.Item,
                                Quantity = $"{ingredient.Qty} {ingredient.Unit}",
                                Category = ingredient.Category,
                                EstimatedCost = ingredient.EstimatedCost,
                                UsedInRecipes = new List<string> { meal.Recipe.Name }
                            });
                        }
                    }
                }
            }

            var groceryList = new GroceryList
            {
                Id = Guid.NewGuid().ToString(),
                Items = groceryItems,
                TotalEstimatedCost = groceryItems.Sum(i => i.EstimatedCost)
            };

            // Group items by category
            groceryList.CategorizedItems = groceryItems
                .GroupBy(i => i.Category)
                .ToDictionary(g => g.Key, g => g.ToList());

            mealPlan.GroceryList = groceryList;
            await _context.SaveChangesAsync();

            return groceryList;
        }

        public async Task<List<GroceryItem>> OptimizeShoppingListAsync(List<GroceryItem> items)
        {
            // Simple optimization: group by category and sort by name
            return items
                .OrderBy(i => i.Category)
                .ThenBy(i => i.Name)
                .ToList();
        }

        public async Task<StoreLocationInfo> FindNearbyStoresAsync(string address)
        {
            // Mock implementation - in reality would integrate with mapping services
            await Task.Delay(100); // Simulate API call

            return new StoreLocationInfo
            {
                NearbyStores = new List<Store>
                {
                    new Store 
                    { 
                        Name = "Whole Foods Market",
                        Address = "123 Main St",
                        Distance = 0.5m,
                        Phone = "(555) 123-4567"
                    },
                    new Store
                    {
                        Name = "Safeway",
                        Address = "456 Oak Ave",
                        Distance = 1.2m,
                        Phone = "(555) 987-6543"
                    }
                },
                UserAddress = address
            };
        }
    }

    public class StoreLocationInfo
    {
        public List<Store> NearbyStores { get; set; } = new();
        public string UserAddress { get; set; } = string.Empty;
    }

    public class Store
    {
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public decimal Distance { get; set; } // in miles
        public string Phone { get; set; } = string.Empty;
    }
}
