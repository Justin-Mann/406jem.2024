import { Component, signal, ChangeDetectionStrategy } from '@angular/core';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

@Component({
  selector: 'app-spinner',
  standalone: true,
  imports: [MatProgressSpinnerModule],
  templateUrl: './spinner.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './spinner.component.css'
})
export class SpinnerComponent {
  isLoading = signal(false);

  showSpinner(loading: boolean) {
    this.isLoading.set(loading);
  }
}
