import { Component, inject, signal, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatListModule } from '@angular/material/list';
import { ResumePostersDataService } from '../services/data/resume-posters-data.service';
import { ResumePoster } from '../interfaces/auth.interface';

@Component({
  selector: 'app-resume-posters',
  standalone: true,
  imports: [FormsModule, MatCardModule, MatFormFieldModule, MatInputModule, MatButtonModule, MatListModule],
  templateUrl: './resume-posters.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './resume-posters.component.css'
})
export class ResumePostersComponent implements OnInit {
  private dataService = inject(ResumePostersDataService);

  posters = signal<ResumePoster[] | null>(null);
  openPosterId = signal<string | null>(null);
  replyToEmail = '';
  message = '';
  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);
  isBusy = signal(false);

  ngOnInit(): void {
    this.dataService.list().subscribe({
      next: items => this.posters.set(items),
      error: () => this.posters.set([])
    });
  }

  toggleForm(posterId: string): void {
    this.errorMessage.set(null);
    this.successMessage.set(null);
    this.message = '';
    this.replyToEmail = '';
    this.openPosterId.set(this.openPosterId() === posterId ? null : posterId);
  }

  onSubmit(posterId: string): void {
    if (!this.replyToEmail.trim() || !this.message.trim()) {
      return;
    }
    this.isBusy.set(true);
    this.errorMessage.set(null);
    this.successMessage.set(null);

    this.dataService.contact(posterId, { message: this.message, replyToEmail: this.replyToEmail }).subscribe({
      next: () => {
        this.isBusy.set(false);
        this.successMessage.set('Message sent!');
        this.message = '';
        this.replyToEmail = '';
      },
      error: () => {
        this.isBusy.set(false);
        this.errorMessage.set('Could not send your message. Please try again.');
      }
    });
  }
}
