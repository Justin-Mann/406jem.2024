import { Component, inject, signal, computed, ChangeDetectionStrategy } from '@angular/core';
import { RouterLink, RouterLinkActive, Router, NavigationEnd } from '@angular/router';
import { filter } from 'rxjs';
import { toSignal } from '@angular/core/rxjs-interop';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatButtonModule } from '@angular/material/button';
import { MatMenuModule } from '@angular/material/menu';
import { AuthService } from '../services/auth/auth.service';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, MatToolbarModule, MatButtonModule, MatMenuModule],
  templateUrl: './header.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './header.component.css'
})
export class HeaderComponent {
  private router = inject(Router);
  authService = inject(AuthService);

  menuOpen = signal(false);
  menuIcon = computed(() => this.menuOpen() ? 'bi bi-x' : 'bi bi-list');

  private navEnd = toSignal(
    this.router.events.pipe(filter(e => e instanceof NavigationEnd))
  );

  isResumePage = computed(() => {
    this.navEnd();
    return this.router.url.includes('/digitalresume');
  });

  toggleMenu() {
    this.menuOpen.update(v => !v);
  }

  closeMenu() {
    this.menuOpen.set(false);
  }

  logout() {
    this.authService.logout();
    this.closeMenu();
    this.router.navigateByUrl('/home');
  }
}
