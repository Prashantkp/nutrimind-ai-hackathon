import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { GroceryListManagerComponent } from './components/grocery-list-manager/grocery-list-manager.component';

const routes: Routes = [
  { path: '', component: GroceryListManagerComponent }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class GroceryListsRoutingModule { }
