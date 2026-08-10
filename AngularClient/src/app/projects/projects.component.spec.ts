import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { ProjectsComponent } from './projects.component';
import { ProjectListingsDataService } from '../services/data/project-listings-data.service';
import { AuthService } from '../services/auth/auth.service';
import { ProjectListing, ProjectListingDto } from '../interfaces/project-listing.interface';

const publicListing: ProjectListing = {
  title: 'Projects',
  sections: [
    { heading: 'WWW', lastUpdated: '04/2025', links: [{ label: 'GitHub', url: 'https://github.com/Justin-Mann' }] }
  ]
};

const myListings: ProjectListingDto[] = [
  { id: '1', ownerUserId: 'admin', isFeatured: true, payload: publicListing, createdAtUtc: '', updatedAtUtc: '' }
];

describe('ProjectsComponent', () => {
  let component: ProjectsComponent;
  let fixture: ComponentFixture<ProjectsComponent>;
  let dataServiceSpy: jasmine.SpyObj<ProjectListingsDataService>;
  let authServiceSpy: jasmine.SpyObj<AuthService>;

  beforeEach(async () => {
    dataServiceSpy = jasmine.createSpyObj('ProjectListingsDataService', ['getPublic', 'listMine', 'create', 'update', 'delete']);
    dataServiceSpy.getPublic.and.returnValue(of(publicListing));
    dataServiceSpy.listMine.and.returnValue(of(myListings));
    authServiceSpy = jasmine.createSpyObj('AuthService', ['isAuthenticated', 'isAdmin']);
    authServiceSpy.isAdmin.and.returnValue(false);

    await TestBed.configureTestingModule({
      imports: [ProjectsComponent],
      providers: [
        { provide: ProjectListingsDataService, useValue: dataServiceSpy },
        { provide: AuthService, useValue: authServiceSpy }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(ProjectsComponent);
    component = fixture.componentInstance;
  });

  it('should create and load the public listing', () => {
    fixture.detectChanges();

    expect(component).toBeTruthy();
    expect(dataServiceSpy.getPublic).toHaveBeenCalledTimes(1);
    expect(component.listing()).toEqual(publicListing);
  });

  it('renders sections and links from data', () => {
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;

    expect(compiled.textContent).toContain('WWW');
    expect(compiled.querySelector('a[href="https://github.com/Justin-Mann"]')).not.toBeNull();
  });

  it('does not show the manage panel for a non-admin visitor', () => {
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;

    expect(compiled.textContent).not.toContain('Manage My Project Listings');
  });

  it('shows the manage panel entry point for an admin', () => {
    authServiceSpy.isAdmin.and.returnValue(true);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;

    expect(compiled.textContent).toContain('Manage My Project Listings');
  });

  it('loads the admin\'s own listings when the manage panel opens', () => {
    authServiceSpy.isAdmin.and.returnValue(true);
    fixture.detectChanges();

    component.openManagePanel();

    expect(dataServiceSpy.listMine).toHaveBeenCalledTimes(1);
    expect(component.mine()).toEqual(myListings);
  });

  it('starts a new listing with one empty section and link', () => {
    component.startNew();

    const state = component.editState();
    expect(state?.id).toBeNull();
    expect(state?.payload.sections.length).toBe(1);
    expect(state?.payload.sections[0].links.length).toBe(1);
  });

  it('creates a listing and reloads on save', () => {
    dataServiceSpy.create.and.returnValue(of(myListings[0]));
    component.startNew();

    component.save();

    expect(dataServiceSpy.create).toHaveBeenCalledTimes(1);
    expect(component.editState()).toBeNull();
    expect(dataServiceSpy.getPublic).toHaveBeenCalled();
  });

  it('updates an existing listing on save', () => {
    dataServiceSpy.update.and.returnValue(of(myListings[0]));
    component.startEdit(myListings[0]);

    component.save();

    expect(dataServiceSpy.update).toHaveBeenCalledWith('1', jasmine.objectContaining({ isFeatured: true }));
  });

  it('shows an error message when saving fails', () => {
    dataServiceSpy.create.and.returnValue(throwError(() => new Error('boom')));
    component.startNew();

    component.save();

    expect(component.errorMessage()).toContain('Could not save');
    expect(component.editState()).not.toBeNull();
  });

  it('deletes a listing and reloads', () => {
    dataServiceSpy.delete.and.returnValue(of(undefined));

    component.deleteListing('1');

    expect(dataServiceSpy.delete).toHaveBeenCalledWith('1');
    expect(dataServiceSpy.listMine).toHaveBeenCalled();
  });

  it('reorders sections when moved up or down', () => {
    component.startNew();
    const state = component.editState()!;
    state.payload.sections.push({ heading: 'Second', links: [] });
    component.editState.set({ ...state });

    component.moveSectionDown(0);

    expect(component.editState()!.payload.sections[0].heading).toBe('Second');
  });

  it('reorders links within a section when moved up or down', () => {
    component.startNew();
    const state = component.editState()!;
    state.payload.sections[0].links.push({ label: 'Second', url: 'https://example.com' });
    component.editState.set({ ...state });

    component.moveLinkDown(component.editState()!.payload.sections[0], 0);

    expect(component.editState()!.payload.sections[0].links[0].label).toBe('Second');
  });

  it('adds and removes a section', () => {
    component.startNew();
    component.addSection();
    expect(component.editState()!.payload.sections.length).toBe(2);

    component.removeSection(1);
    expect(component.editState()!.payload.sections.length).toBe(1);
  });

  it('adds and removes a link', () => {
    component.startNew();
    const section = component.editState()!.payload.sections[0];
    component.addLink(section);
    expect(component.editState()!.payload.sections[0].links.length).toBe(2);

    component.removeLink(component.editState()!.payload.sections[0], 1);
    expect(component.editState()!.payload.sections[0].links.length).toBe(1);
  });
});
