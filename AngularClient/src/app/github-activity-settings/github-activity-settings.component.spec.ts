import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { GitHubActivitySettingsComponent } from './github-activity-settings.component';
import { GitHubActivitySettingsDataService } from '../services/data/github-activity-settings-data.service';
import { AuthService } from '../services/auth/auth.service';
import { GitHubActivitySettingsDto } from '../interfaces/github-activity-settings.interface';

const mySettings: GitHubActivitySettingsDto = {
  enabled: true,
  gitHubUsername: 'justin-mann',
  repoCount: 5,
  pinnedRepoNames: ['406jem.2026'],
};

describe('GitHubActivitySettingsComponent', () => {
  let component: GitHubActivitySettingsComponent;
  let fixture: ComponentFixture<GitHubActivitySettingsComponent>;
  let dataServiceSpy: jasmine.SpyObj<GitHubActivitySettingsDataService>;
  let authServiceSpy: jasmine.SpyObj<AuthService>;

  beforeEach(async () => {
    dataServiceSpy = jasmine.createSpyObj('GitHubActivitySettingsDataService', ['getMine', 'updateMine']);
    dataServiceSpy.getMine.and.returnValue(of(mySettings));
    authServiceSpy = jasmine.createSpyObj('AuthService', ['isAuthenticated', 'isAdmin', 'isSuperAdmin']);
    authServiceSpy.isAdmin.and.returnValue(true);

    await TestBed.configureTestingModule({
      imports: [GitHubActivitySettingsComponent],
      providers: [
        { provide: GitHubActivitySettingsDataService, useValue: dataServiceSpy },
        { provide: AuthService, useValue: authServiceSpy }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(GitHubActivitySettingsComponent);
    component = fixture.componentInstance;
  });

  it('should create and load the admin\'s settings', () => {
    fixture.detectChanges();

    expect(component).toBeTruthy();
    expect(dataServiceSpy.getMine).toHaveBeenCalledTimes(1);
    expect(component.settings()).toEqual(mySettings);
  });

  it('shows an access-required message for a non-admin visitor', () => {
    authServiceSpy.isAdmin.and.returnValue(false);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;

    expect(compiled.textContent).toContain('Resume Admin access required');
  });

  it('shows an error and falls back to empty settings when loading fails', () => {
    dataServiceSpy.getMine.and.returnValue(throwError(() => new Error('boom')));
    fixture.detectChanges();

    expect(component.errorMessage()).toContain('Could not load');
    expect(component.settings()?.enabled).toBeFalse();
  });

  it('adds and removes a pinned repo', () => {
    fixture.detectChanges();

    component.newPinnedRepo.set('another-repo');
    component.addPinnedRepo();
    expect(component.settings()!.pinnedRepoNames).toEqual(['406jem.2026', 'another-repo']);
    expect(component.newPinnedRepo()).toBe('');

    component.removePinnedRepo(0);
    expect(component.settings()!.pinnedRepoNames).toEqual(['another-repo']);
  });

  it('does not add a blank pinned repo', () => {
    fixture.detectChanges();

    component.newPinnedRepo.set('   ');
    component.addPinnedRepo();

    expect(component.settings()!.pinnedRepoNames).toEqual(['406jem.2026']);
  });

  it('saves settings and shows a success message', () => {
    fixture.detectChanges();
    const saved: GitHubActivitySettingsDto = { ...mySettings, enabled: false };
    dataServiceSpy.updateMine.and.returnValue(of(saved));

    component.save();

    expect(dataServiceSpy.updateMine).toHaveBeenCalledWith({
      enabled: true,
      gitHubUsername: 'justin-mann',
      repoCount: 5,
      pinnedRepoNames: ['406jem.2026'],
    });
    expect(component.settings()).toEqual(saved);
    expect(component.statusMessage()).toContain('saved');
  });

  it('shows an error message when saving fails', () => {
    fixture.detectChanges();
    dataServiceSpy.updateMine.and.returnValue(throwError(() => new Error('boom')));

    component.save();

    expect(component.errorMessage()).toContain('Could not save');
  });
});
