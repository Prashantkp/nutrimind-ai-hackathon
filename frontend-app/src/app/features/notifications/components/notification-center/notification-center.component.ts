import { Component } from '@angular/core';

@Component({
  selector: 'app-notification-center',
  template: `
    <div class="notification-center">
      <h1>Notifications</h1>
      <p>Notification center coming soon...</p>
    </div>
  `,
  styles: [`
    .notification-center {
      padding: 2rem;
      text-align: center;
    }
  `]
})
export class NotificationCenterComponent {}
