import { Component, inject, signal, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatListModule } from '@angular/material/list';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatDividerModule } from '@angular/material/divider';
import { ProjectListingsDataService } from '../services/data/project-listings-data.service';
import { AuthService } from '../services/auth/auth.service';
import { CreateOrUpdateProjectListingRequest, ProjectLink, ProjectListing, ProjectListingDto, ProjectSection } from '../interfaces/project-listing.interface';
import { GitHubActivityComponent } from '../github-activity/github-activity.component';

interface ListingEditState {
  id: string | null;
  isFeatured: boolean;
  payload: ProjectListing;
}

@Component({
  selector: 'app-projects',
  standalone: true,
  imports: [
    FormsModule,
    MatCardModule,
    MatButtonModule,
    MatListModule,
    MatFormFieldModule,
    MatInputModule,
    MatCheckboxModule,
    MatDividerModule,
    GitHubActivityComponent
  ],
  templateUrl: './projects.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './projects.component.css'
})
export class ProjectsComponent implements OnInit {
  private dataService = inject(ProjectListingsDataService);
  authService = inject(AuthService);

  listing = signal<ProjectListing | null>(null);
  showManagePanel = signal(false);
  mine = signal<ProjectListingDto[] | null>(null);
  editState = signal<ListingEditState | null>(null);
  errorMessage = signal<string | null>(null);
  isBusy = signal(false);

  ngOnInit(): void {
    this.loadPublic();
  }

  private loadPublic(): void {
    this.dataService.getPublic().subscribe({
      next: listing => this.listing.set(listing),
      error: () => this.listing.set({ sections: [] })
    });
  }

  openManagePanel(): void {
    this.showManagePanel.set(true);
    this.loadMine();
  }

  private loadMine(): void {
    this.errorMessage.set(null);
    this.dataService.listMine().subscribe({
      next: items => this.mine.set(items),
      error: () => {
        this.errorMessage.set('Could not load your project listings.');
        this.mine.set([]);
      }
    });
  }

  startNew(): void {
    this.editState.set({
      id: null,
      isFeatured: false,
      payload: {
        title: 'Projects',
        sections: [this.emptySection()]
      }
    });
  }

  startEdit(item: ProjectListingDto): void {
    const payload = item.payload ?? { sections: [] };
    this.editState.set({
      id: item.id,
      isFeatured: item.isFeatured,
      payload: {
        title: payload.title,
        sections: (payload.sections ?? []).map(s => ({
          heading: s.heading,
          lastUpdated: s.lastUpdated,
          links: (s.links ?? []).map(l => ({ label: l.label, url: l.url, description: l.description }))
        }))
      }
    });
  }

  cancelEdit(): void {
    this.editState.set(null);
  }

  addSection(): void {
    const state = this.editState();
    if (!state) return;
    state.payload.sections.push(this.emptySection());
    this.editState.set({ ...state });
  }

  removeSection(index: number): void {
    const state = this.editState();
    if (!state) return;
    state.payload.sections.splice(index, 1);
    this.editState.set({ ...state });
  }

  moveSectionUp(index: number): void {
    this.moveSection(index, index - 1);
  }

  moveSectionDown(index: number): void {
    this.moveSection(index, index + 1);
  }

  addLink(section: ProjectSection): void {
    section.links.push(this.emptyLink());
    this.touchEditState();
  }

  removeLink(section: ProjectSection, index: number): void {
    section.links.splice(index, 1);
    this.touchEditState();
  }

  moveLinkUp(section: ProjectSection, index: number): void {
    this.moveLink(section, index, index - 1);
  }

  moveLinkDown(section: ProjectSection, index: number): void {
    this.moveLink(section, index, index + 1);
  }

  save(): void {
    const state = this.editState();
    if (!state) return;

    this.isBusy.set(true);
    this.errorMessage.set(null);

    const request: CreateOrUpdateProjectListingRequest = { isFeatured: state.isFeatured, payload: state.payload };
    const result$ = state.id
      ? this.dataService.update(state.id, request)
      : this.dataService.create(request);

    result$.subscribe({
      next: () => {
        this.isBusy.set(false);
        this.editState.set(null);
        this.loadMine();
        this.loadPublic();
      },
      error: () => {
        this.isBusy.set(false);
        this.errorMessage.set('Could not save this project listing. Please try again.');
      }
    });
  }

  deleteListing(id: string): void {
    this.isBusy.set(true);
    this.errorMessage.set(null);

    this.dataService.delete(id).subscribe({
      next: () => {
        this.isBusy.set(false);
        this.loadMine();
        this.loadPublic();
      },
      error: () => {
        this.isBusy.set(false);
        this.errorMessage.set('Could not delete this project listing. Please try again.');
      }
    });
  }

  private emptySection(): ProjectSection {
    return { heading: '', links: [this.emptyLink()] };
  }

  private emptyLink(): ProjectLink {
    return { label: '', url: '' };
  }

  private moveSection(a: number, b: number): void {
    const state = this.editState();
    if (!state || b < 0 || b >= state.payload.sections.length) return;
    [state.payload.sections[a], state.payload.sections[b]] = [state.payload.sections[b], state.payload.sections[a]];
    this.editState.set({ ...state });
  }

  private moveLink(section: ProjectSection, a: number, b: number): void {
    if (b < 0 || b >= section.links.length) return;
    [section.links[a], section.links[b]] = [section.links[b], section.links[a]];
    this.touchEditState();
  }

  private touchEditState(): void {
    const state = this.editState();
    if (state) {
      this.editState.set({ ...state });
    }
  }
}
