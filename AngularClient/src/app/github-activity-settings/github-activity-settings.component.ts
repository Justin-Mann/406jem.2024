import { Component, inject, signal, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { GitHubActivitySettingsDataService } from '../services/data/github-activity-settings-data.service';
import { AuthService } from '../services/auth/auth.service';
import { emptyGitHubActivitySettings, GitHubActivitySettingsDto } from '../interfaces/github-activity-settings.interface';

@Component({
  selector: 'app-github-activity-settings',
  standalone: true,
  imports: [
    FormsModule,
    MatCardModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatSlideToggleModule,
  ],
  templateUrl: './github-activity-settings.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './github-activity-settings.component.css',
})
export class GitHubActivitySettingsComponent implements OnInit {
  private dataService = inject(GitHubActivitySettingsDataService);
  authService = inject(AuthService);

  settings = signal<GitHubActivitySettingsDto | null>(null);
  newPinnedRepo = signal('');
  errorMessage = signal<string | null>(null);
  statusMessage = signal<string | null>(null);
  isBusy = signal(false);

  ngOnInit(): void {
    this.load();
  }

  private load(): void {
    this.errorMessage.set(null);
    this.dataService.getMine().subscribe({
      next: settings => this.settings.set(settings),
      error: () => {
        this.errorMessage.set('Could not load your GitHub Activity settings.');
        this.settings.set(emptyGitHubActivitySettings());
      }
    });
  }

  addPinnedRepo(): void {
    const name = this.newPinnedRepo().trim();
    const settings = this.settings();
    if (!name || !settings) return;

    this.settings.set({ ...settings, pinnedRepoNames: [...settings.pinnedRepoNames, name] });
    this.newPinnedRepo.set('');
  }

  removePinnedRepo(index: number): void {
    const settings = this.settings();
    if (!settings) return;

    this.settings.set({ ...settings, pinnedRepoNames: settings.pinnedRepoNames.filter((_, i) => i !== index) });
  }

  save(): void {
    const settings = this.settings();
    if (!settings) return;

    this.isBusy.set(true);
    this.errorMessage.set(null);
    this.statusMessage.set(null);

    this.dataService.updateMine({
      enabled: settings.enabled,
      gitHubUsername: settings.gitHubUsername,
      repoCount: settings.repoCount,
      pinnedRepoNames: settings.pinnedRepoNames,
    }).subscribe({
      next: saved => {
        this.isBusy.set(false);
        this.settings.set(saved);
        this.statusMessage.set('GitHub Activity settings saved.');
      },
      error: () => {
        this.isBusy.set(false);
        this.errorMessage.set('Could not save your GitHub Activity settings. Please try again.');
      }
    });
  }
}
