import { Component, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { AuthService } from '../../services/auth/auth.service';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [FormsModule, RouterLink, MatCardModule, MatFormFieldModule, MatInputModule, MatButtonModule],
  templateUrl: './register.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './register.component.css'
})
export class RegisterComponent {
  private authService = inject(AuthService);
  private router = inject(Router);

  username = '';
  email = '';
  password = '';
  errorMessage = signal<string | null>(null);
  isBusy = signal(false);

  onSubmit(): void {
    this.isBusy.set(true);
    this.errorMessage.set(null);

    this.authService.register({ username: this.username, email: this.email, password: this.password }).subscribe(error => {
      if (error) {
        this.isBusy.set(false);
        this.errorMessage.set(error);
        return;
      }

      this.authService.login({ username: this.username, password: this.password }).subscribe(loginError => {
        this.isBusy.set(false);
        if (loginError) {
          this.errorMessage.set(loginError);
        } else {
          this.router.navigateByUrl('/');
        }
      });
    });
  }
}
