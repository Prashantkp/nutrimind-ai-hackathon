import { Component } from '@angular/core';

@Component({
  selector: 'app-root',
  template: `
    <div class="app-container">
      <app-header *ngIf="showHeader"></app-header>
      <main class="main-content" [class.with-header]="showHeader">
        <router-outlet></router-outlet>
      </main>
      <app-loading *ngIf="isLoading"></app-loading>
    </div>
  `,
  styles: [`
    .app-container {
      min-height: 100vh;
      display: flex;
      flex-direction: column;
    }

    .main-content {
      flex: 1;
      transition: all 0.3s ease;
    }

    .main-content.with-header {
      padding-top: 64px;
    }

    @media (max-width: 768px) {
      .main-content.with-header {
        padding-top: 56px;
      }
    }
  `]
})
export class AppComponent {
  title = 'NutriMind - Your AI Nutrition Companion';
  isLoading = false;

  get showHeader(): boolean {
    // Hide header on login/register pages
    return !window.location.pathname.includes('/auth');
  }
}
