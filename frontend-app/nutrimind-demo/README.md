# NutriMind Demo

A static HTML/CSS/JavaScript demo of the NutriMind meal planning application, designed for demonstration purposes without API integration.

## Overview

NutriMind is a personalized nutrition assistant that helps users plan healthy meals tailored to their dietary preferences, nutritional goals, and budget constraints.

## Demo Pages

### 1. Registration Page (`register.html`)
- **Purpose**: User account creation
- **Features**: 
  - Animated floating label inputs
  - Form validation
  - Modern gradient design
  - Mobile-responsive layout
  - Smooth animations and transitions

### 2. Profile Setup (`profile.html`)
- **Purpose**: Comprehensive user profile configuration
- **Features**:
  - Interactive sliders for height, weight, budget, cooking time
  - Custom checkbox selections for dietary preferences
  - Tag-based input for allergies and dislikes
  - Progress bar showing completion status
  - Responsive grid layouts

### 3. Dashboard (`dashboard.html`)
- **Purpose**: Main meal planning interface
- **Features**:
  - Weekly meal plan overview
  - Day-by-day meal cards with nutrition info
  - Recipe detail modals
  - Interactive meal actions
  - Responsive design for mobile and desktop
  - Rich animations and hover effects

## Design Features

### Color Scheme
- **Primary Green**: `#4CAF50` - Represents health and nature
- **Secondary Green**: `#81C784` - Lighter accent
- **Accent Orange**: `#FF8A65` - For highlights and CTAs
- **Neutral Grays**: Various shades for text and backgrounds

### Typography
- **Font Family**: Inter (Google Fonts)
- **Weights**: 300, 400, 500, 600, 700
- **Hierarchy**: Clear visual hierarchy with consistent sizing

### Animation & Interactions
- **CSS Transitions**: Smooth 0.3s cubic-bezier transitions
- **Hover Effects**: Subtle transforms and shadow changes
- **Loading States**: Custom spinners and state indicators
- **Touch Feedback**: Mobile-optimized touch interactions

## File Structure

```
nutrimind-demo/
├── register.html          # Registration page
├── profile.html           # Profile setup page
├── dashboard.html         # Main dashboard
├── css/
│   └── main.css          # Main stylesheet with design system
├── js/
│   └── common.js         # Utility functions and interactions
└── assets/
    └── (placeholder for images)
```

## Sample Data

The dashboard includes realistic sample meal data featuring:

- **Breakfast Options**: Avocado toast, Greek yogurt parfait, overnight oats
- **Lunch Options**: Mediterranean quinoa bowl, Caesar wraps, turkey wraps
- **Dinner Options**: Grilled salmon, vegetable stir fry, beef and broccoli
- **Nutrition Info**: Calories, protein, cost, difficulty, cooking time
- **Interactive Elements**: Recipe viewing, meal marking, plan regeneration

## Key Interactive Features

### Registration Form
- Real-time form validation
- Password confirmation checking
- Smooth form submission animation
- Redirect flow to profile setup

### Profile Setup
- Interactive range sliders with live value updates
- Multi-select dietary preferences
- Dynamic tag addition/removal for allergies
- Form progress tracking
- Comprehensive data collection

### Dashboard
- Week navigation tabs
- Meal detail modal windows
- Action buttons for each meal
- Nutritional overview statistics
- Floating action button for new plans
- Responsive day cards

## Responsive Design

- **Mobile First**: Optimized for mobile devices
- **Breakpoints**: 768px (tablet), 480px (mobile)
- **Flexible Layouts**: CSS Grid and Flexbox
- **Touch-Friendly**: Large tap targets and gestures
- **Performance**: Optimized animations and transitions

## Usage Instructions

### Local Development
1. Open any HTML file directly in a web browser
2. No server required - all functionality is client-side
3. Use browser developer tools to inspect animations and interactions

### Navigation Flow
1. Start with `register.html`
2. Progress to `profile.html`
3. End at `dashboard.html` for the main experience

### Browser Compatibility
- **Modern Browsers**: Chrome, Firefox, Safari, Edge
- **CSS Features**: Grid, Flexbox, Custom Properties, Transforms
- **JavaScript**: ES6+ features, DOM manipulation

## Demo Scenarios

### User Registration
1. Enter user details with validation feedback
2. Experience smooth form animations
3. See loading states and success messages

### Profile Configuration
1. Adjust sliders for physical stats
2. Select dietary preferences with visual feedback
3. Add/remove allergies with tag interface
4. Watch progress bar update in real-time

### Meal Plan Interaction
1. Browse weekly meal plans
2. View detailed recipe information
3. Mark meals as completed
4. Navigate between different weeks
5. Experience responsive design on different devices

## Customization

### Colors
Modify CSS custom properties in `main.css`:
```css
:root {
  --primary-green: #4CAF50;
  --primary-green-dark: #45a049;
  /* ... other colors */
}
```

### Animation Speed
Adjust transition durations:
```css
:root {
  --transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
}
```

### Meal Data
Modify the `mealData` object in `dashboard.html` to change meal information.

## Future Enhancements

For a production version, consider:
- API integration for dynamic data
- User authentication system
- Meal plan generation algorithms
- Shopping list functionality
- Nutritional analysis
- Social sharing features
- Offline support with service workers

## Technical Notes

- **No Dependencies**: Pure HTML, CSS, and JavaScript
- **Modern CSS**: Uses CSS Grid, Flexbox, and Custom Properties
- **Accessible**: Semantic HTML and keyboard navigation support
- **Performance**: Optimized for smooth 60fps animations
- **SEO-Friendly**: Semantic markup and proper heading hierarchy

This demo showcases the user experience and visual design of the NutriMind application without requiring backend integration or API connections.
