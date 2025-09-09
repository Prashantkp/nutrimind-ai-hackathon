import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';

import { NotificationsRoutingModule } from './notifications-routing.module';
import { NotificationCenterComponent } from './components/notification-center/notification-center.component';

@NgModule({
  declarations: [
    NotificationCenterComponent
  ],
  imports: [
    CommonModule,
    NotificationsRoutingModule,
    MatCardModule,
    MatIconModule,
    MatButtonModule
  ]
})
export class NotificationsModule { }
