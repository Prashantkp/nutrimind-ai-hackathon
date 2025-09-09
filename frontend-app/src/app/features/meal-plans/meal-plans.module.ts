import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule } from '@angular/material/dialog';

import { MealPlansRoutingModule } from './meal-plans-routing.module';
import { MealPlanCalendarComponent } from './components/meal-plan-calendar/meal-plan-calendar.component';
import { SharedModule } from '../../shared/shared.module';

@NgModule({
  declarations: [
    MealPlanCalendarComponent
  ],
  imports: [
    CommonModule,
    MealPlansRoutingModule,
    SharedModule,
    MatCardModule,
    MatIconModule,
    MatButtonModule,
    MatDialogModule
  ]
})
export class MealPlansModule { }
