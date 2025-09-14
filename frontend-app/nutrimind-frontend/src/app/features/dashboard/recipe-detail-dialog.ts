import { Component, Inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatCheckboxModule } from '@angular/material/checkbox';

@Component({
  selector: 'app-recipe-detail-dialog',
  standalone: true,
  imports: [
    CommonModule,
    MatDialogModule,
    MatButtonModule,
    MatIconModule,
    MatChipsModule,
    MatCheckboxModule
  ],
  templateUrl: './recipe-detail-dialog.html',
  styleUrl: './recipe-detail-dialog.scss'
})
export class RecipeDetailDialogComponent {
  
  constructor(
    public dialogRef: MatDialogRef<RecipeDetailDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: any
  ) {
    console.log('Recipe data:', this.data);
  }

  closeDialog(): void {
    this.dialogRef.close();
  }

  formatTime(minutes: number | undefined): string {
    if (!minutes) return 'N/A';
    
    if (minutes < 60) {
      return `${minutes}m`;
    } else {
      const hours = Math.floor(minutes / 60);
      const mins = minutes % 60;
      return mins > 0 ? `${hours}h ${mins}m` : `${hours}h`;
    }
  }
}
