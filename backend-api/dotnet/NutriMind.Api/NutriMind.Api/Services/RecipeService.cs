using Microsoft.EntityFrameworkCore;
using NutriMind.Api.Data;
using NutriMind.Api.Models;

namespace NutriMind.Api.Services
{
    public interface IRecipeService
    {
        Task<Recipe?> GetRecipeAsync(string recipeId);
        Task<List<Recipe>> SearchRecipesAsync(string query, RecipeSearchFilters? filters = null);
        Task<List<Recipe>> GetRecommendedRecipesAsync(UserProfile userProfile);
        Task<Recipe> CreateRecipeAsync(Recipe recipe);
        Task<Recipe?> UpdateRecipeAsync(Recipe recipe);
        Task DeleteRecipeAsync(string recipeId);
        Task<List<Recipe>> GetRecipesByTypeAsync(string mealType);
        Task<List<Recipe>> GetRecipesByCuisineAsync(string cuisineType);
    }

    public class RecipeService : IRecipeService
    {
        private readonly NutriMindDbContext _context;

        public RecipeService(NutriMindDbContext context)
        {
            _context = context;
        }

        public async Task<Recipe?> GetRecipeAsync(string recipeId)
        {
            return await _context.Recipes
                .FirstOrDefaultAsync(r => r.Id == recipeId);
        }

        public async Task<List<Recipe>> SearchRecipesAsync(string query, RecipeSearchFilters? filters = null)
        {
            var recipesQuery = _context.Recipes.AsQueryable();

            if (!string.IsNullOrEmpty(query))
            {
                recipesQuery = recipesQuery.Where(r => 
                    r.Name.Contains(query) || 
                    r.Tags.Any(t => t.Contains(query)));
            }

            if (filters != null)
            {
                if (!string.IsNullOrEmpty(filters.CuisineType))
                {
                    recipesQuery = recipesQuery.Where(r => r.Cuisine == filters.CuisineType);
                }

                if (!string.IsNullOrEmpty(filters.DifficultyLevel))
                {
                    recipesQuery = recipesQuery.Where(r => r.Difficulty == filters.DifficultyLevel);
                }

                if (filters.CookTime.HasValue)
                {
                    recipesQuery = recipesQuery.Where(r => r.TotalTime <= filters.CookTime.Value);
                }

                if (filters.DietaryRestrictions != null && filters.DietaryRestrictions.Any())
                {
                    foreach (var restriction in filters.DietaryRestrictions)
                    {
                        switch (restriction.ToLower())
                        {
                            case "vegan":
                                recipesQuery = recipesQuery.Where(r => r.IsVegan);
                                break;
                            case "keto":
                                recipesQuery = recipesQuery.Where(r => r.IsKeto);
                                break;
                            case "diabetic":
                                recipesQuery = recipesQuery.Where(r => r.IsDiabeticFriendly);
                                break;
                        }
                    }
                }
            }

            return await recipesQuery.Take(50).ToListAsync();
        }

        public async Task<List<Recipe>> GetRecommendedRecipesAsync(UserProfile userProfile)
        {
            var recipesQuery = _context.Recipes.AsQueryable();

            // Filter by dietary preferences
            if (!string.IsNullOrEmpty(userProfile.DietaryPreference))
            {
                switch (userProfile.DietaryPreference.ToLower())
                {
                    case "vegan":
                        recipesQuery = recipesQuery.Where(r => r.IsVegan);
                        break;
                    case "keto":
                        recipesQuery = recipesQuery.Where(r => r.IsKeto);
                        break;
                    case "diabetic":
                        recipesQuery = recipesQuery.Where(r => r.IsDiabeticFriendly);
                        break;
                }
            }

            // Filter by preferred cuisines
            if (userProfile.PreferredCuisines.Any())
            {
                recipesQuery = recipesQuery.Where(r => 
                    userProfile.PreferredCuisines.Any(pc => r.Cuisine.ToLower().Contains(pc.ToLower())));
            }

            // Filter by cooking time preference
            recipesQuery = recipesQuery.Where(r => r.TotalTime <= userProfile.CookingTimePreference);

            // Filter by cooking skill level
            switch (userProfile.CookingSkillLevel.ToLower())
            {
                case "beginner":
                    recipesQuery = recipesQuery.Where(r => r.Difficulty == "easy");
                    break;
                case "intermediate":
                    recipesQuery = recipesQuery.Where(r => r.Difficulty == "easy" || r.Difficulty == "medium");
                    break;
                // Advanced can see all recipes
            }

            return await recipesQuery.Take(20).ToListAsync();
        }

        public async Task<Recipe> CreateRecipeAsync(Recipe recipe)
        {
            recipe.Id = Guid.NewGuid().ToString();
            
            _context.Recipes.Add(recipe);
            await _context.SaveChangesAsync();
            return recipe;
        }

        public async Task<Recipe?> UpdateRecipeAsync(Recipe recipe)
        {
            var existingRecipe = await GetRecipeAsync(recipe.Id);
            if (existingRecipe == null)
                return null;

            existingRecipe.Name = recipe.Name;
            existingRecipe.Cuisine = recipe.Cuisine;
            existingRecipe.Steps = recipe.Steps;
            existingRecipe.Ingredients = recipe.Ingredients;
            existingRecipe.IsVegan = recipe.IsVegan;
            existingRecipe.IsKeto = recipe.IsKeto;
            existingRecipe.IsDiabeticFriendly = recipe.IsDiabeticFriendly;
            existingRecipe.Calories = recipe.Calories;
            existingRecipe.Tags = recipe.Tags;
            existingRecipe.Source = recipe.Source;
            existingRecipe.Difficulty = recipe.Difficulty;
            existingRecipe.TotalTime = recipe.TotalTime;
            existingRecipe.Servings = recipe.Servings;

            await _context.SaveChangesAsync();
            return existingRecipe;
        }

        public async Task DeleteRecipeAsync(string recipeId)
        {
            var recipe = await GetRecipeAsync(recipeId);
            if (recipe != null)
            {
                _context.Recipes.Remove(recipe);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<Recipe>> GetRecipesByTypeAsync(string mealType)
        {
            // For now, search by tags since Recipe doesn't have MealType property
            return await _context.Recipes
                .Where(r => r.Tags.Any(t => t.ToLower().Contains(mealType.ToLower())))
                .ToListAsync();
        }

        public async Task<List<Recipe>> GetRecipesByCuisineAsync(string cuisineType)
        {
            return await _context.Recipes
                .Where(r => r.Cuisine.ToLower().Contains(cuisineType.ToLower()))
                .ToListAsync();
        }
    }

    public class RecipeSearchFilters
    {
        public string? MealType { get; set; }
        public string? CuisineType { get; set; }
        public string? DifficultyLevel { get; set; }
        public int? CookTime { get; set; }
        public List<string>? DietaryRestrictions { get; set; }
    }
}