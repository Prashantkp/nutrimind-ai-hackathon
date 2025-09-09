import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { NotificationCenterComponent } from './components/notification-center/notification-center.component';

const routes: Routes = [
  { path: '', component: NotificationCenterComponent }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class NotificationsRoutingModule { }
