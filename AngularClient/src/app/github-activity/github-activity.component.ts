import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { GitHubActivityDataService } from '../services/data/github-activity-data.service';
import { GitHubRepo } from '../interfaces/github-activity.interface';

/** Projects page card (#68) - renders itself entirely, including "render nothing" when the
 * configured owner has the feature disabled/unconfigured or either fetch fails. */
@Component({
  selector: 'app-github-activity',
  standalone: true,
  imports: [MatCardModule],
  templateUrl: './github-activity.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './github-activity.component.css'
})
export class GitHubActivityComponent implements OnInit {
  private dataService = inject(GitHubActivityDataService);

  loading = signal(true);
  repos = signal<GitHubRepo[] | null>(null);

  ngOnInit(): void {
    this.dataService.getActivity().subscribe({
      next: repos => {
        this.repos.set(repos && repos.length > 0 ? repos : null);
        this.loading.set(false);
      },
      error: () => {
        this.repos.set(null);
        this.loading.set(false);
      }
    });
  }
}
