import { Component, OnInit } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { AuthService } from '../../../../core/services/auth.service';
import { GroceryList, GroceryListItem, User } from '../../../../core/models';

@Component({
  selector: 'app-grocery-list-manager',
  templateUrl: './grocery-list-manager.component.html',
  styleUrls: ['./grocery-list-manager.component.scss']
})
export class GroceryListManagerComponent implements OnInit {
  currentUser: User | null = null;
  groceryLists: GroceryList[] = [];
  activeList: GroceryList | null = null;
  isLoading = false;
  newItemName = '';
  selectedListIndex = 0;
  selectedCategories: string[] = [];
  quickAddSuggestions: string[] = ['Milk', 'Bread', 'Eggs', 'Chicken', 'Bananas'];

  // Categories for organizing items
  categories = [
    { name: 'Produce', icon: 'eco', color: '#4CAF50' },
    { name: 'Meat & Seafood', icon: 'set_meal', color: '#F44336' },
    { name: 'Dairy & Eggs', icon: 'egg_alt', color: '#2196F3' },
    { name: 'Pantry', icon: 'kitchen', color: '#FF9800' },
    { name: 'Frozen', icon: 'ac_unit', color: '#00BCD4' },
    { name: 'Bakery', icon: 'cake', color: '#9C27B0' },
    { name: 'Beverages', icon: 'local_drink', color: '#607D8B' },
    { name: 'Snacks', icon: 'cookie', color: '#795548' },
    { name: 'Health & Beauty', icon: 'spa', color: '#E91E63' },
    { name: 'Other', icon: 'shopping_cart', color: '#9E9E9E' }
  ];

  constructor(
    private authService: AuthService,
    private dialog: MatDialog,
    private snackBar: MatSnackBar
  ) {}

  ngOnInit(): void {
    this.authService.currentUser$.subscribe(user => {
      this.currentUser = user;
      if (user) {
        this.loadGroceryLists();
      }
    });
  }

  private loadGroceryLists(): void {
    if (!this.currentUser) return;
    
    this.isLoading = true;
    
    // Mock data - replace with actual API call
    setTimeout(() => {
      this.groceryLists = [
        {
          id: '1',
          userId: this.currentUser!.id,
          name: 'Weekly Shopping',
          description: 'Regular weekly grocery shopping',
          items: this.generateMockItems(),
          isActive: true,
          totalEstimatedCost: 127.50,
          createdAt: new Date(Date.now() - 2 * 24 * 60 * 60 * 1000), // 2 days ago
          updatedAt: new Date()
        },
        {
          id: '2',
          userId: this.currentUser!.id,
          name: 'Party Supplies',
          description: 'Items for weekend party',
          items: [
            {
              id: '1',
              groceryListId: '2',
              ingredientName: 'Chips',
              quantity: 3,
              unit: 'bags',
              category: 'Snacks',
              isCompleted: false,
              estimatedCost: 12.99,
              notes: 'Get variety pack'
            },
            {
              id: '2',
              groceryListId: '2',
              ingredientName: 'Soda',
              quantity: 2,
              unit: 'bottles',
              category: 'Beverages',
              isCompleted: false,
              estimatedCost: 8.99
            }
          ],
          isActive: false,
          totalEstimatedCost: 21.98,
          createdAt: new Date(Date.now() - 5 * 24 * 60 * 60 * 1000), // 5 days ago
          updatedAt: new Date(Date.now() - 3 * 24 * 60 * 60 * 1000)
        }
      ];
      
      this.activeList = this.groceryLists.find(list => list.isActive) || this.groceryLists[0];
      this.isLoading = false;
    }, 1000);
  }

  private generateMockItems(): GroceryListItem[] {
    return [
      {
        id: '1',
        groceryListId: '1',
        ingredientName: 'Organic Spinach',
        quantity: 1,
        unit: 'bag',
        category: 'Produce',
        isCompleted: true,
        estimatedCost: 3.99
      },
      {
        id: '2',
        groceryListId: '1',
        ingredientName: 'Chicken Breast',
        quantity: 2,
        unit: 'lbs',
        category: 'Meat & Seafood',
        isCompleted: false,
        estimatedCost: 12.99,
        notes: 'Organic if available'
      },
      {
        id: '3',
        groceryListId: '1',
        ingredientName: 'Greek Yogurt',
        quantity: 1,
        unit: 'container',
        category: 'Dairy & Eggs',
        isCompleted: false,
        estimatedCost: 5.49
      },
      {
        id: '4',
        groceryListId: '1',
        ingredientName: 'Brown Rice',
        quantity: 1,
        unit: 'bag',
        category: 'Pantry',
        isCompleted: false,
        estimatedCost: 4.99
      },
      {
        id: '5',
        groceryListId: '1',
        ingredientName: 'Salmon Fillet',
        quantity: 1,
        unit: 'lb',
        category: 'Meat & Seafood',
        isCompleted: false,
        estimatedCost: 15.99
      },
      {
        id: '6',
        groceryListId: '1',
        ingredientName: 'Avocados',
        quantity: 4,
        unit: 'pieces',
        category: 'Produce',
        isCompleted: true,
        estimatedCost: 6.00
      }
    ];
  }

  getItemsByCategory(): { [key: string]: GroceryListItem[] } {
    if (!this.activeList) return {};
    
    const grouped: { [key: string]: GroceryListItem[] } = {};
    
    this.activeList.items.forEach(item => {
      const category = item.category || 'Other';
      if (!grouped[category]) {
        grouped[category] = [];
      }
      grouped[category].push(item);
    });
    
    // Sort items within each category: uncompleted first, then by name
    Object.keys(grouped).forEach(category => {
      grouped[category].sort((a, b) => {
        if (a.isCompleted !== b.isCompleted) {
          return a.isCompleted ? 1 : -1;
        }
        return a.ingredientName.localeCompare(b.ingredientName);
      });
    });
    
    return grouped;
  }

  getCategoryInfo(categoryName: string) {
    return this.categories.find(cat => cat.name === categoryName) || this.categories[this.categories.length - 1];
  }

  getCompletionStats(): { completed: number; total: number; percentage: number } {
    if (!this.activeList) return { completed: 0, total: 0, percentage: 0 };
    
    const total = this.activeList.items.length;
    const completed = this.activeList.items.filter(item => item.isCompleted).length;
    const percentage = total > 0 ? Math.round((completed / total) * 100) : 0;
    
    return { completed, total, percentage };
  }

  toggleItemCompletion(item: GroceryListItem): void {
    item.isCompleted = !item.isCompleted;
    
    // Update in backend
    console.log('Toggle item completion:', item.id, item.isCompleted);
    
    this.snackBar.open(
      item.isCompleted ? 'Item marked as completed' : 'Item marked as incomplete',
      'Undo',
      { duration: 3000 }
    ).onAction().subscribe(() => {
      // Undo action
      item.isCompleted = !item.isCompleted;
    });
  }

  addNewItem(): void {
    if (!this.newItemName.trim() || !this.activeList) return;
    
    const newItem: GroceryListItem = {
      id: Date.now().toString(),
      groceryListId: this.activeList.id,
      ingredientName: this.newItemName.trim(),
      quantity: 1,
      unit: 'piece',
      category: 'Other',
      isCompleted: false,
      estimatedCost: 0
    };
    
    this.activeList.items.push(newItem);
    this.newItemName = '';
    
    // Save to backend
    console.log('Add new item:', newItem);
    
    this.snackBar.open('Item added to list', 'Close', { duration: 2000 });
  }

  editItem(item: GroceryListItem): void {
    // Will implement item editing dialog
    console.log('Edit item:', item.id);
  }

  deleteItem(item: GroceryListItem): void {
    if (!this.activeList) return;
    
    const index = this.activeList.items.findIndex(i => i.id === item.id);
    if (index > -1) {
      this.activeList.items.splice(index, 1);
      
      // Delete from backend
      console.log('Delete item:', item.id);
      
      this.snackBar.open('Item removed from list', 'Undo', { duration: 3000 })
        .onAction().subscribe(() => {
          // Undo deletion
          this.activeList!.items.splice(index, 0, item);
        });
    }
  }

  selectList(list: GroceryList): void {
    this.activeList = list;
  }

  createNewList(): void {
    // Will implement list creation dialog
    console.log('Create new grocery list');
  }

  duplicateList(list: GroceryList): void {
    const newList: GroceryList = {
      ...list,
      id: Date.now().toString(),
      name: `${list.name} (Copy)`,
      isActive: false,
      items: list.items.map(item => ({
        ...item,
        id: `${item.id}-copy`,
        groceryListId: Date.now().toString(),
        isCompleted: false
      })),
      createdAt: new Date(),
      updatedAt: new Date()
    };
    
    this.groceryLists.unshift(newList);
    this.snackBar.open('List duplicated successfully', 'Close', { duration: 2000 });
  }

  deleteList(list: GroceryList): void {
    const index = this.groceryLists.findIndex(l => l.id === list.id);
    if (index > -1) {
      this.groceryLists.splice(index, 1);
      
      // If we deleted the active list, select another one
      if (this.activeList?.id === list.id) {
        this.activeList = this.groceryLists.length > 0 ? this.groceryLists[0] : null;
      }
      
      this.snackBar.open('List deleted', 'Undo', { duration: 3000 })
        .onAction().subscribe(() => {
          // Undo deletion
          this.groceryLists.splice(index, 0, list);
          if (!this.activeList) {
            this.activeList = list;
          }
        });
    }
  }

  generateFromMealPlan(): void {
    // Will implement meal plan integration
    console.log('Generate list from meal plan');
  }

  shareList(list: GroceryList): void {
    // Will implement list sharing
    console.log('Share list:', list.id);
  }

  exportList(list: GroceryList): void {
    // Will implement list export (email, print, etc.)
    console.log('Export list:', list.id);
  }

  clearCompletedItems(): void {
    if (!this.activeList) return;
    
    const completedItems = this.activeList.items.filter(item => item.isCompleted);
    this.activeList.items = this.activeList.items.filter(item => !item.isCompleted);
    
    this.snackBar.open(
      `${completedItems.length} completed items cleared`,
      'Undo',
      { duration: 3000 }
    ).onAction().subscribe(() => {
      // Undo clearing
      this.activeList!.items.push(...completedItems);
    });
  }

  // Additional methods for simplified template
  getCompletionPercentage(): number {
    if (!this.activeList || this.activeList.items.length === 0) return 0;
    return Math.round((this.getCompletedItemsCount() / this.activeList.items.length) * 100);
  }

  getCompletedItemsCount(): number {
    if (!this.activeList) return 0;
    return this.activeList.items.filter(item => item.isCompleted).length;
  }

  addItem(): void {
    this.addNewItem();
  }

  removeItem(item: GroceryListItem): void {
    this.deleteItem(item);
  }

  getCompletedCount(list: GroceryList): number {
    return list.items.filter(item => item.isCompleted).length;
  }

  editList(list: GroceryList): void {
    console.log('Edit list:', list.name);
    // Will implement list editing functionality
  }
}
