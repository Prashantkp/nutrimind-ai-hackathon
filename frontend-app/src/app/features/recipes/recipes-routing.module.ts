import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { RecipeSearchComponent } from './components/recipe-search/recipe-search.component';

const routes: Routes = [
  { path: '', component: RecipeSearchComponent },
  { path: 'search', component: RecipeSearchComponent }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class RecipesRoutingModule { }
