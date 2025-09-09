import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-loading',
  templateUrl: './loading.component.html',
  styleUrls: ['./loading.component.scss']
})
export class LoadingComponent {
  @Input() message: string = 'Loading...';
  @Input() size: 'small' | 'medium' | 'large' = 'medium';
  @Input() overlay: boolean = true;

  getSpinnerSize(): number {
    switch (this.size) {
      case 'small': return 40;
      case 'large': return 80;
      default: return 60;
    }
  }
}
