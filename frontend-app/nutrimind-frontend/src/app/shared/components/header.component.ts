import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [
    CommonModule,
    MatToolbarModule,
    MatButtonModule,
    MatIconModule,
    MatMenuModule
  ],
  template: `
    <mat-toolbar color="primary" class="app-toolbar">
      <span class="app-title">
        <mat-icon>restaurant</mat-icon>
        NutriMind AI
      </span>
      
      <span class="spacer"></span>
      
      <div class="user-menu">
        @if (currentUser(); as user) {
          <span class="user-greeting">
            Hello, {{ user.firstName || user.email }}
          </span>
          
          <button mat-icon-button [matMenuTriggerFor]="userMenu">
            <mat-icon>account_circle</mat-icon>
          </button>
          
          <mat-menu #userMenu="matMenu">
            <button mat-menu-item (click)="logout()">
              <mat-icon>logout</mat-icon>
              <span>Logout</span>
            </button>
          </mat-menu>
        } @else {
          <button mat-button (click)="logout()">
            <mat-icon>logout</mat-icon>
            Logout
          </button>
        }
      </div>
    </mat-toolbar>
  `,
  styles: [`
    .app-toolbar {
      position: sticky;
      top: 0;
      z-index: 100;
    }
    
    .app-title {
      display: flex;
      align-items: center;
      gap: 8px;
      font-size: 20px;
      font-weight: 500;
    }
    
    .spacer {
      flex: 1 1 auto;
    }
    
    .user-menu {
      display: flex;
      align-items: center;
      gap: 16px;
    }
    
    .user-greeting {
      font-size: 14px;
      opacity: 0.9;
    }
    
    @media (max-width: 768px) {
      .user-greeting {
        display: none;
      }
      
      .app-title {
        font-size: 18px;
      }
    }
  `]
})
export class HeaderComponent {
  private authService = inject(AuthService);
  
  currentUser = this.authService.currentUser;

  logout(): void {
    this.authService.logout();
  }
}
