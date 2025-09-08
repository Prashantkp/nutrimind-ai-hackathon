using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using NutriMind.Api.Models;
using NutriMind.Api.Helpers;

namespace NutriMind.Api.Activities
{
    public class ComputeGroceryListActivity
    {
        private readonly ILogger<ComputeGroceryListActivity> _logger;

        public ComputeGroceryListActivity(ILogger<ComputeGroceryListActivity> logger)
        {
            _logger = logger;
        }

        [Function("ComputeGroceryList")]
        public async Task<GroceryList> Run([ActivityTrigger] MealPlan mealPlan)
        {
            try
            {
                _logger.LogInformation("Computing grocery list for meal plan: {MealPlanId}", mealPlan.Id);

                // Use the helper to consolidate ingredients from all meals
                var groceryList = GroceryListHelper.ConsolidateIngredients(mealPlan.DailyMeals);

                // Apply additional processing
                groceryList = await EnhanceGroceryList(groceryList);

                // Estimate total cost
                groceryList.TotalEstimatedCost = groceryList.Items.Sum(item => item.EstimatedCost);

                // Update meal plan with grocery list reference
                mealPlan.GroceryList = groceryList;
                mealPlan.TotalEstimatedCost = groceryList.TotalEstimatedCost;

                _logger.LogInformation("Computed grocery list with {ItemCount} items, estimated cost: ${EstimatedCost:F2}", 
                    groceryList.Items.Count, groceryList.TotalEstimatedCost);

                return groceryList;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error computing grocery list for meal plan: {MealPlanId}", mealPlan.Id);
                throw;
            }
        }

        private async Task<GroceryList> EnhanceGroceryList(GroceryList groceryList)
        {
            // Sort items by category for better shopping experience
            var categoryOrder = new Dictionary<string, int>
            {
                ["produce"] = 1,
                ["dairy"] = 2,
                ["meat"] = 3,
                ["protein"] = 3,
                ["seafood"] = 4,
                ["grains"] = 5,
                ["pantry"] = 6,
                ["spices"] = 7,
                ["frozen"] = 8,
                ["other"] = 9
            };

            groceryList.Items = groceryList.Items
                .OrderBy(item => categoryOrder.ContainsKey(item.Category.ToLower()) 
                    ? categoryOrder[item.Category.ToLower()] 
                    : 9)
                .ThenBy(item => item.Name)
                .ToList();

            // Re-categorize items for better organization
            var categorizedItems = new Dictionary<string, List<GroceryItem>>();
            
            foreach (var item in groceryList.Items)
            {
                var category = NormalizeCategory(item.Category);
                item.Category = category;

                if (!categorizedItems.ContainsKey(category))
                    categorizedItems[category] = new List<GroceryItem>();

                categorizedItems[category].Add(item);
            }

            groceryList.CategorizedItems = categorizedItems;

            // Apply cost estimation improvements
            ApplyCostEstimationRefinements(groceryList);

            return groceryList;
        }

        private string NormalizeCategory(string category)
        {
            var normalized = category.ToLower().Trim();
            
            return normalized switch
            {
                var c when c.Contains("produce") || c.Contains("vegetable") || c.Contains("fruit") => "Produce",
                var c when c.Contains("dairy") || c.Contains("milk") || c.Contains("cheese") => "Dairy & Eggs",
                var c when c.Contains("meat") || c.Contains("beef") || c.Contains("chicken") || c.Contains("pork") => "Meat & Poultry",
                var c when c.Contains("seafood") || c.Contains("fish") || c.Contains("salmon") => "Seafood",
                var c when c.Contains("grain") || c.Contains("bread") || c.Contains("pasta") || c.Contains("rice") => "Grains & Bread",
                var c when c.Contains("pantry") || c.Contains("oil") || c.Contains("sauce") || c.Contains("canned") => "Pantry Staples",
                var c when c.Contains("spice") || c.Contains("herb") || c.Contains("seasoning") => "Herbs & Spices",
                var c when c.Contains("frozen") => "Frozen Foods",
                var c when c.Contains("beverage") || c.Contains("drink") => "Beverages",
                var c when c.Contains("snack") || c.Contains("nuts") => "Snacks & Nuts",
                _ => "Other"
            };
        }

        private void ApplyCostEstimationRefinements(GroceryList groceryList)
        {
            // Apply regional cost adjustments, seasonal pricing, and bulk discounts
            foreach (var item in groceryList.Items)
            {
                // Apply seasonal adjustments (simplified)
                var seasonalMultiplier = GetSeasonalMultiplier(item);
                
                // Apply bulk discount for items used in multiple recipes
                var bulkDiscount = item.UsedInRecipes.Count > 3 ? 0.9m : 1.0m;

                // Apply category-specific cost adjustments
                var categoryMultiplier = GetCategoryMultiplier(item.Category);

                item.EstimatedCost = item.EstimatedCost * seasonalMultiplier * bulkDiscount * categoryMultiplier;
                
                // Round to reasonable precision
                item.EstimatedCost = Math.Round(item.EstimatedCost, 2);
            }
        }

        private decimal GetSeasonalMultiplier(GroceryItem item)
        {
            var currentMonth = DateTime.Now.Month;
            var itemName = item.Name.ToLower();

            // Simplified seasonal pricing (would be more complex in production)
            if (item.Category == "Produce")
            {
                // Winter months (Dec, Jan, Feb) - higher prices for most produce
                if (currentMonth is 12 or 1 or 2)
                {
                    if (itemName.Contains("berry") || itemName.Contains("tomato"))
                        return 1.3m; // 30% higher in winter
                    else if (itemName.Contains("apple") || itemName.Contains("carrot"))
                        return 0.9m; // 10% lower (storage crops)
                }
                // Summer months (Jun, Jul, Aug) - lower prices for most produce
                else if (currentMonth is 6 or 7 or 8)
                {
                    return 0.8m; // 20% lower in summer
                }
            }

            return 1.0m; // No adjustment
        }

        private decimal GetCategoryMultiplier(string category)
        {
            return category switch
            {
                "Seafood" => 1.2m, // Premium category
                "Herbs & Spices" => 1.1m, // Often expensive per unit
                "Meat & Poultry" => 1.1m, // Premium category
                "Pantry Staples" => 0.9m, // Often bulk/discount items
                "Grains & Bread" => 0.9m, // Often bulk/discount items
                _ => 1.0m
            };
        }
    }
}
