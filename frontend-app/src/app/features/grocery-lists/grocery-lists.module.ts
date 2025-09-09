import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatChipsModule } from '@angular/material/chips';
import { MatDialogModule } from '@angular/material/dialog';
import { MatSnackBarModule } from '@angular/material/snack-bar';
import { MatMenuModule } from '@angular/material/menu';

import { GroceryListsRoutingModule } from './grocery-lists-routing.module';
import { GroceryListManagerComponent } from './components/grocery-list-manager/grocery-list-manager.component';
import { SharedModule } from '../../shared/shared.module';

@NgModule({
  declarations: [
    GroceryListManagerComponent
  ],
  imports: [
    CommonModule,
    FormsModule,
    GroceryListsRoutingModule,
    SharedModule,
    MatCardModule,
    MatIconModule,
    MatButtonModule,
    MatCheckboxModule,
    MatFormFieldModule,
    MatInputModule,
    MatChipsModule,
    MatDialogModule,
    MatSnackBarModule,
    MatMenuModule
  ]
})
export class GroceryListsModule { }
