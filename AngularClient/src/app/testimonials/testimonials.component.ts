import { Component, inject, signal, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatListModule } from '@angular/material/list';
import { MatDividerModule } from '@angular/material/divider';
import { TestimonialsDataService } from '../services/data/testimonials-data.service';
import { AuthService } from '../services/auth/auth.service';
import { Testimonial } from '../interfaces/auth.interface';

@Component({
  selector: 'app-testimonials',
  standalone: true,
  imports: [FormsModule, RouterLink, MatCardModule, MatFormFieldModule, MatInputModule, MatButtonModule, MatListModule, MatDividerModule],
  templateUrl: './testimonials.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './testimonials.component.css'
})
export class TestimonialsComponent implements OnInit {
  private dataService = inject(TestimonialsDataService);
  authService = inject(AuthService);

  testimonials = signal<Testimonial[] | null>(null);
  newMessage = '';
  errorMessage = signal<string | null>(null);
  isBusy = signal(false);

  ngOnInit(): void {
    this.load();
  }

  private load(): void {
    this.dataService.list().subscribe({
      next: items => this.testimonials.set(items),
      error: () => this.testimonials.set([])
    });
  }

  onSubmit(): void {
    if (!this.newMessage.trim()) {
      return;
    }
    this.isBusy.set(true);
    this.errorMessage.set(null);

    this.dataService.create(this.newMessage).subscribe({
      next: () => {
        this.newMessage = '';
        this.isBusy.set(false);
        this.load();
      },
      error: () => {
        this.isBusy.set(false);
        this.errorMessage.set('Could not post your testimonial. Please try again.');
      }
    });
  }

  onDelete(id: string): void {
    this.dataService.delete(id).subscribe({ next: () => this.load() });
  }
}
