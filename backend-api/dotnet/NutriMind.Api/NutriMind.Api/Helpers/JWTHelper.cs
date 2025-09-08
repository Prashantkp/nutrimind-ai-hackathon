using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace NutriMind.Api.Helpers
{
    public class JWTHelper
    {
        private readonly string _secretKey;
        private readonly string _issuer;
        private readonly string _audience;

        public JWTHelper(string secretKey, string issuer, string audience)
        {
            _secretKey = secretKey;
            _issuer = issuer;
            _audience = audience;
        }

        public string GenerateToken(string userId, string email, Dictionary<string, string>? additionalClaims = null)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_secretKey);

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, userId),
                new(ClaimTypes.Email, email),
                new("user_id", userId),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            };

            if (additionalClaims != null)
            {
                foreach (var claim in additionalClaims)
                {
                    claims.Add(new Claim(claim.Key, claim.Value));
                }
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddDays(7), // Token valid for 7 days
                Issuer = _issuer,
                Audience = _audience,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        public ClaimsPrincipal? ValidateToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(_secretKey);

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _issuer,
                    ValidateAudience = true,
                    ValidAudience = _audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                var principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);
                return principal;
            }
            catch
            {
                return null;
            }
        }

        public string? GetUserIdFromToken(string token)
        {
            var principal = ValidateToken(token);
            return principal?.FindFirst("user_id")?.Value;
        }

        public string? GetEmailFromToken(string token)
        {
            var principal = ValidateToken(token);
            return principal?.FindFirst(ClaimTypes.Email)?.Value;
        }

        public bool IsTokenExpired(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var jsonToken = tokenHandler.ReadJwtToken(token);
                
                return jsonToken.ValidTo < DateTime.UtcNow;
            }
            catch
            {
                return true; // If we can't read the token, consider it expired
            }
        }

        public string RefreshToken(string expiredToken)
        {
            var principal = ValidateToken(expiredToken);
            if (principal == null)
                throw new ArgumentException("Invalid token");

            var userId = principal.FindFirst("user_id")?.Value;
            var email = principal.FindFirst(ClaimTypes.Email)?.Value;

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(email))
                throw new ArgumentException("Invalid token claims");

            return GenerateToken(userId, email);
        }
    }

    public static class WeekHelper
    {
        public static string GetWeekIdentifier(DateTime date)
        {
            var calendar = System.Globalization.CultureInfo.InvariantCulture.Calendar;
            var weekNumber = calendar.GetWeekOfYear(date, System.Globalization.CalendarWeekRule.FirstDay, DayOfWeek.Monday);
            return $"{date.Year}-W{weekNumber:D2}";
        }

        public static DateTime GetMondayOfWeek(string weekIdentifier)
        {
            // Parse format: YYYY-Www (e.g., 2025-W37)
            var parts = weekIdentifier.Split('-');
            if (parts.Length != 2 || !parts[1].StartsWith('W'))
                throw new ArgumentException("Invalid week identifier format. Expected YYYY-Www");

            if (!int.TryParse(parts[0], out int year) || !int.TryParse(parts[1][1..], out int week))
                throw new ArgumentException("Invalid week identifier format");

            var jan1 = new DateTime(year, 1, 1);
            var daysOffset = DayOfWeek.Thursday - jan1.DayOfWeek;
            var firstThursday = jan1.AddDays(daysOffset);
            var cal = System.Globalization.CultureInfo.CurrentCulture.Calendar;
            var firstWeek = cal.GetWeekOfYear(firstThursday, System.Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
            
            var weekNum = week;
            if (firstWeek <= 1)
            {
                weekNum -= 1;
            }
            
            var result = firstThursday.AddDays(weekNum * 7);
            return result.AddDays(-3); // Go back to Monday
        }

        public static bool IsValidWeekIdentifier(string weekIdentifier)
        {
            try
            {
                GetMondayOfWeek(weekIdentifier);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    public static class NutritionCalculator
    {
        public static Models.NutritionSummary CalculateWeeklyNutrition(Dictionary<string, Models.DailyMealPlan> dailyMeals)
        {
            var summary = new Models.NutritionSummary();
            
            if (!dailyMeals.Any())
                return summary;

            int totalCalories = 0;
            decimal totalProtein = 0;
            decimal totalCarbs = 0;
            decimal totalFats = 0;

            foreach (var day in dailyMeals.Values)
            {
                totalCalories += day.DailyNutrition.TotalCalories;
                totalProtein += day.DailyNutrition.TotalProtein;
                totalCarbs += day.DailyNutrition.TotalCarbs;
                totalFats += day.DailyNutrition.TotalFats;
            }

            summary.TotalCalories = totalCalories;
            summary.TotalProtein = totalProtein;
            summary.TotalCarbs = totalCarbs;
            summary.TotalFats = totalFats;
            summary.AverageDailyCalories = totalCalories / Math.Max(dailyMeals.Count, 1);

            return summary;
        }

        public static Models.NutritionInfo CalculateMealNutrition(Models.Recipe recipe, int servings = 1)
        {
            var baseNutrition = new Models.NutritionInfo
            {
                Calories = recipe.Calories,
                // These would typically come from a nutrition database
                // For now, using rough estimates based on calories
                Protein = recipe.Calories * 0.15m / 4, // 15% of calories from protein
                Carbohydrates = recipe.Calories * 0.50m / 4, // 50% from carbs
                Fats = recipe.Calories * 0.35m / 9, // 35% from fats
                Fiber = recipe.Calories * 0.014m, // Rough estimate
                Sugar = recipe.Calories * 0.10m / 4, // 10% from sugar
                Sodium = recipe.Calories * 2.5m // Rough estimate
            };

            // Scale by servings
            if (servings != 1)
            {
                baseNutrition.Calories = (int)(baseNutrition.Calories * servings);
                baseNutrition.Protein *= servings;
                baseNutrition.Carbohydrates *= servings;
                baseNutrition.Fats *= servings;
                baseNutrition.Fiber *= servings;
                baseNutrition.Sugar *= servings;
                baseNutrition.Sodium *= servings;
            }

            return baseNutrition;
        }
    }

    public static class GroceryListHelper
    {
        public static Models.GroceryList ConsolidateIngredients(Dictionary<string, Models.DailyMealPlan> dailyMeals)
        {
            var groceryList = new Models.GroceryList
            {
                Id = Guid.NewGuid().ToString()
            };

            var consolidatedItems = new Dictionary<string, Models.GroceryItem>();
            var categoryItems = new Dictionary<string, List<Models.GroceryItem>>();

            foreach (var day in dailyMeals.Values)
            {
                var allMeals = day.Meals.Values.ToList();
                allMeals.AddRange(day.Snacks);

                foreach (var meal in allMeals)
                {
                    foreach (var ingredient in meal.Recipe.Ingredients)
                    {
                        var key = ingredient.Item.ToLowerInvariant().Trim();
                        
                        if (consolidatedItems.TryGetValue(key, out var existingItem))
                        {
                            // Combine quantities (simplified - would need unit conversion in production)
                            existingItem.UsedInRecipes.Add(meal.Recipe.Name);
                            existingItem.EstimatedCost += ingredient.EstimatedCost;
                        }
                        else
                        {
                            var groceryItem = new Models.GroceryItem
                            {
                                Name = ingredient.Item,
                                Quantity = ingredient.Qty,
                                Unit = ingredient.Unit,
                                Category = ingredient.Category,
                                EstimatedCost = ingredient.EstimatedCost,
                                UsedInRecipes = new List<string> { meal.Recipe.Name }
                            };
                            
                            consolidatedItems[key] = groceryItem;
                        }
                    }
                }
            }

            groceryList.Items = consolidatedItems.Values.ToList();
            groceryList.TotalEstimatedCost = groceryList.Items.Sum(i => i.EstimatedCost);

            // Categorize items
            foreach (var item in groceryList.Items)
            {
                if (!categoryItems.ContainsKey(item.Category))
                    categoryItems[item.Category] = new List<Models.GroceryItem>();
                
                categoryItems[item.Category].Add(item);
            }

            groceryList.CategorizedItems = categoryItems;
            return groceryList;
        }
    }
}
