import { Component, signal } from '@angular/core';

@Component({
  selector: 'app-spinner',
  standalone: true,
  imports: [],
  templateUrl: './spinner.component.html',
  styleUrl: './spinner.component.css'
})
export class SpinnerComponent {
  isLoading = signal(false);

  showSpinner(loading: boolean) {
    this.isLoading.set(loading);
  }
}
