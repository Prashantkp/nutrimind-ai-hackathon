# NutriMind API - Sample Requests

This document provides sample HTTP requests for all NutriMind API endpoints.

## Base URL
```
Local Development: http://localhost:7071
Azure: https://your-function-app.azurewebsites.net
```

## Authentication
Most endpoints require JWT authentication. Include the token in the Authorization header:
```
Authorization: Bearer your-jwt-token-here
```

---

## 1. Profile API (`/api/profile`)

### Create User Profile
```http
POST /api/profile
Content-Type: application/json

{
  "email": "john.doe@example.com",
  "firstName": "John",
  "lastName": "Doe",
  "age": 30,
  "height": 175.5,
  "weight": 70.0,
  "activityLevel": "moderate",
  "dietaryPreference": "vegetarian",
  "allergens": ["nuts", "shellfish"],
  "dislikes": ["mushrooms", "cilantro"],
  "healthGoals": ["weight_loss", "muscle_gain"],
  "targetCalories": 2000,
  "mealFrequency": 3,
  "cookingSkillLevel": "intermediate",
  "cookingTimePreference": 45,
  "budgetPerWeek": 75.00,
  "preferredCuisines": ["mediterranean", "asian", "italian"],
  "notificationPreferences": {
    "emailEnabled": true,
    "pushEnabled": true,
    "mealReminders": true,
    "groceryReminders": true,
    "weeklyPlanReminders": true,
    "nutritionTips": false,
    "reminderTimes": ["08:00", "12:00", "18:00"]
  }
}
```

### Get User Profile
```http
GET /api/profile
Authorization: Bearer your-jwt-token-here
```

### Update User Profile
```http
PUT /api/profile
Authorization: Bearer your-jwt-token-here
Content-Type: application/json

{
  "targetCalories": 2200,
  "cookingSkillLevel": "advanced",
  "budgetPerWeek": 85.00,
  "preferredCuisines": ["mediterranean", "mexican", "thai"]
}
```

### Delete User Profile
```http
DELETE /api/profile
Authorization: Bearer your-jwt-token-here
```

---

## 2. Meal Plans API (`/api/mealplans`)

### Generate New Meal Plan
```http
POST /api/mealplans/generate
Authorization: Bearer your-jwt-token-here
Content-Type: application/json

{
  "weekIdentifier": "2025-W37",
  "dietaryPreference": "vegetarian",
  "allergens": ["nuts"],
  "dislikes": ["mushrooms"],
  "targetCalories": 2000,
  "maxPrepTime": 45,
  "budgetConstraint": 75.00,
  "varietyLevel": "high",
  "regenerateExisting": false
}
```

### Get All Meal Plans
```http
GET /api/mealplans
Authorization: Bearer your-jwt-token-here
```

### Get Specific Meal Plan
```http
GET /api/mealplans/2025-W37
Authorization: Bearer your-jwt-token-here
```

### Update Meal Plan
```http
PUT /api/mealplans/meal-plan-id-123
Authorization: Bearer your-jwt-token-here
Content-Type: application/json

{
  "dailyMeals": {
    "monday": {
      "breakfast": {
        "recipeId": "recipe-456",
        "servings": 1,
        "scheduledTime": "08:00"
      },
      "lunch": {
        "recipeId": "recipe-789",
        "servings": 1,
        "scheduledTime": "12:30"
      },
      "dinner": {
        "recipeId": "recipe-101",
        "servings": 2,
        "scheduledTime": "18:00"
      }
    }
  }
}
```

### Delete Meal Plan
```http
DELETE /api/mealplans/meal-plan-id-123
Authorization: Bearer your-jwt-token-here
```

### Check Meal Plan Generation Status
```http
GET /api/mealplans/status/orchestration-id-abc123
Authorization: Bearer your-jwt-token-here
```

---

## 3. Recipes API (`/api/recipes`)

### Search Recipes
```http
GET /api/recipes/search?query=chicken&cuisineType=mediterranean&maxPrepTime=30&dietaryRestrictions=gluten-free&page=1&pageSize=20
Authorization: Bearer your-jwt-token-here
```

### Get Recipe by ID
```http
GET /api/recipes/recipe-123
Authorization: Bearer your-jwt-token-here
```

### Get Recipe Suggestions
```http
POST /api/recipes/suggestions
Authorization: Bearer your-jwt-token-here
Content-Type: application/json

{
  "mealType": "dinner",
  "cuisinePreference": "italian",
  "maxPrepTime": 45,
  "excludeIngredients": ["mushrooms", "cilantro"],
  "targetCalories": 600,
  "servings": 2
}
```

### Rate Recipe
```http
POST /api/recipes/recipe-123/rate
Authorization: Bearer your-jwt-token-here
Content-Type: application/json

{
  "rating": 4,
  "review": "Delicious and easy to make! My family loved it."
}
```

---

## 4. Integrations API (`/api/integrations`)

### Connect to Grocery Provider
```http
POST /api/integrations/connect
Authorization: Bearer your-jwt-token-here
Content-Type: application/json

{
  "provider": "instacart",
  "redirectUri": "https://your-app.com/callback"
}
```

### Handle OAuth Callback
```http
POST /api/integrations/oauth/callback
Authorization: Bearer your-jwt-token-here
Content-Type: application/json

{
  "provider": "instacart",
  "code": "auth-code-from-provider",
  "state": "security-state-token"
}
```

### Get Connected Integrations
```http
GET /api/integrations
Authorization: Bearer your-jwt-token-here
```

### Disconnect Provider
```http
DELETE /api/integrations/instacart
Authorization: Bearer your-jwt-token-here
```

---

## 5. Grocery API (`/api/grocery`)

### Create Grocery Checkout
```http
POST /api/grocery/checkout
Authorization: Bearer your-jwt-token-here
Content-Type: application/json

{
  "mealPlanId": "meal-plan-123",
  "provider": "instacart",
  "selectedItemIds": ["milk", "eggs", "chicken breast", "broccoli"],
  "deliveryDate": "2025-09-10",
  "deliveryTime": "10:00-12:00"
}
```

### Get Grocery List for Meal Plan
```http
GET /api/grocery/meal-plan-123
Authorization: Bearer your-jwt-token-here
```

### Update Grocery List
```http
PUT /api/grocery/meal-plan-123
Authorization: Bearer your-jwt-token-here
Content-Type: application/json

{
  "items": [
    {
      "name": "Organic Chicken Breast",
      "quantity": 2,
      "unit": "lbs",
      "category": "Meat & Seafood",
      "estimatedCost": 12.99,
      "isOrganic": true,
      "recipeIds": ["recipe-101", "recipe-205"],
      "notes": "Free-range preferred"
    },
    {
      "name": "Fresh Broccoli",
      "quantity": 3,
      "unit": "heads",
      "category": "Produce",
      "estimatedCost": 4.50,
      "isOrganic": false,
      "recipeIds": ["recipe-101"],
      "notes": "Look for bright green color"
    }
  ],
  "totalEstimatedCost": 17.49,
  "lastUpdated": "2025-09-08T15:30:00Z"
}
```

### Get Available Grocery Providers
```http
GET /api/grocery/providers
Authorization: Bearer your-jwt-token-here
```

---

## 6. Notifications API (`/api/notifications`)

### Test Notification
```http
POST /api/notifications/test
Authorization: Bearer your-jwt-token-here
Content-Type: application/json

{
  "type": "email",
  "message": "This is a test notification to verify your settings are working correctly."
}
```

### Send Meal Reminder
```http
POST /api/notifications/meal-reminder
Authorization: Bearer your-jwt-token-here
Content-Type: application/json

{
  "userId": "user-123",
  "mealType": "breakfast",
  "recipeName": "Avocado Toast with Eggs",
  "customMessage": "Don't forget to start your day with a healthy breakfast!",
  "scheduledTime": "2025-09-09T08:00:00Z"
}
```

### Send Grocery Reminder
```http
POST /api/notifications/grocery-reminder
Authorization: Bearer your-jwt-token-here
Content-Type: application/json

{
  "userId": "user-123",
  "mealPlanId": "meal-plan-123",
  "customMessage": "Time to go grocery shopping for this week's meal plan!",
  "suggestedStore": "Whole Foods Market",
  "reminderTime": "2025-09-09T14:00:00Z"
}
```

### Get Notification Settings
```http
GET /api/notifications/settings
Authorization: Bearer your-jwt-token-here
```

### Update Notification Settings
```http
PUT /api/notifications/settings
Authorization: Bearer your-jwt-token-here
Content-Type: application/json

{
  "emailNotifications": true,
  "pushNotifications": true,
  "mealReminders": true,
  "groceryReminders": true,
  "weeklyPlanReminders": false,
  "nutritionTips": true,
  "reminderTimes": ["07:30", "12:00", "17:30"]
}
```

---

## Sample Response Formats

### Success Response
```json
{
  "success": true,
  "data": {
    // Response data here
  },
  "message": "Operation completed successfully",
  "errors": [],
  "timestamp": "2025-09-08T15:30:00.000Z"
}
```

### Error Response
```json
{
  "success": false,
  "data": null,
  "message": "Validation failed",
  "errors": [
    "Email is required",
    "Age must be between 1 and 150"
  ],
  "timestamp": "2025-09-08T15:30:00.000Z"
}
```

### Meal Plan Generation Response
```json
{
  "success": true,
  "data": {
    "orchestrationId": "abc123-def456-ghi789",
    "status": "Running",
    "estimatedCompletionTime": "2025-09-08T15:35:00.000Z",
    "statusCheckUrl": "/api/mealplans/status/abc123-def456-ghi789"
  },
  "message": "Meal plan generation started",
  "errors": [],
  "timestamp": "2025-09-08T15:30:00.000Z"
}
```

### Grocery Checkout Response
```json
{
  "success": true,
  "data": {
    "checkoutUrl": "https://instacart.com/checkout/cart123",
    "cartId": "cart123",
    "provider": "instacart",
    "estimatedTotal": 67.43,
    "itemCount": 12,
    "expiresAt": "2025-09-09T15:30:00.000Z"
  },
  "message": "Checkout created successfully",
  "errors": [],
  "timestamp": "2025-09-08T15:30:00.000Z"
}
```

---

## Testing with cURL

### Example cURL command for creating a profile:
```bash
curl -X POST "http://localhost:7071/api/profile" \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "firstName": "Test",
    "lastName": "User",
    "age": 25,
    "height": 170,
    "weight": 65,
    "activityLevel": "moderate",
    "targetCalories": 2000
  }'
```

### Example cURL command with authentication:
```bash
curl -X GET "http://localhost:7071/api/profile" \
  -H "Authorization: Bearer your-jwt-token-here"
```

---

## Testing with Postman

1. Import these requests into Postman
2. Set up environment variables:
   - `baseUrl`: `http://localhost:7071` (for local) or your Azure Function URL
   - `authToken`: Your JWT token after authentication
3. Use `{{baseUrl}}` and `{{authToken}}` in your requests
4. Set up pre-request scripts for automatic token refresh if needed

---

## Notes

- All endpoints return JSON responses in the standard format shown above
- Authentication is required for all endpoints except the initial profile creation
- Date formats follow ISO 8601 standard (YYYY-MM-DDTHH:mm:ss.sssZ)
- Week identifiers use ISO week format: YYYY-Www (e.g., "2025-W37")
- Meal plan generation is asynchronous - use the status endpoint to check progress
- Provider connections require OAuth flow completion
- All costs are in USD unless specified otherwise
