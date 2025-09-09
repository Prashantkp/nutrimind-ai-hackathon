import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { MealPlanCalendarComponent } from './components/meal-plan-calendar/meal-plan-calendar.component';

const routes: Routes = [
  { path: '', component: MealPlanCalendarComponent }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class MealPlansRoutingModule { }
