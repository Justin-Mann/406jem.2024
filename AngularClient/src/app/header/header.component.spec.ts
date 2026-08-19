import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { HeaderComponent } from './header.component';
import { AuthService } from '../services/auth/auth.service';

describe('HeaderComponent', () => {
  let component: HeaderComponent;
  let fixture: ComponentFixture<HeaderComponent>;
  let authServiceSpy: jasmine.SpyObj<AuthService>;

  beforeEach(async () => {
    authServiceSpy = jasmine.createSpyObj('AuthService', ['isAuthenticated', 'username', 'logout', 'isAdmin', 'isSuperAdmin']);
    authServiceSpy.isAuthenticated.and.returnValue(false);
    authServiceSpy.username.and.returnValue(null);
    authServiceSpy.isAdmin.and.returnValue(false);
    authServiceSpy.isSuperAdmin.and.returnValue(false);

    await TestBed.configureTestingModule({
      imports: [HeaderComponent],
      providers: [provideRouter([]), { provide: AuthService, useValue: authServiceSpy }]
    })
    .compileComponents();

    fixture = TestBed.createComponent(HeaderComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('shows Log In and Register links when not authenticated', () => {
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Log In');
    expect(compiled.textContent).toContain('Register');
  });

  it('shows the username and Log Out link when authenticated', () => {
    authServiceSpy.isAuthenticated.and.returnValue(true);
    authServiceSpy.username.and.returnValue('jane');
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;

    expect(compiled.textContent).toContain('Hi, jane');
    expect(compiled.textContent).toContain('Log Out');
  });

  it('does not show the Admin menu for a non-admin visitor', () => {
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.admin-menu-trigger')).toBeNull();
  });

  it('shows the Admin menu trigger for a ResumeAdmin', () => {
    authServiceSpy.isAdmin.and.returnValue(true);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;

    expect(compiled.querySelector('.admin-menu-trigger')).not.toBeNull();
  });

  it('shows Manage Project Listings in the Admin menu for a SuperAdmin', () => {
    authServiceSpy.isAdmin.and.returnValue(true);
    authServiceSpy.isSuperAdmin.and.returnValue(true);
    fixture.detectChanges();

    (fixture.nativeElement.querySelector('.admin-menu-trigger') as HTMLElement).click();
    fixture.detectChanges();

    expect(document.body.textContent).toContain('Manage Project Listings');
  });

  it('hides Manage Project Listings in the Admin menu for a plain ResumeAdmin', () => {
    authServiceSpy.isAdmin.and.returnValue(true);
    authServiceSpy.isSuperAdmin.and.returnValue(false);
    fixture.detectChanges();

    (fixture.nativeElement.querySelector('.admin-menu-trigger') as HTMLElement).click();
    fixture.detectChanges();

    expect(document.body.textContent).toContain('Manage Resumes');
    expect(document.body.textContent).toContain('GitHub Activity Settings');
    expect(document.body.textContent).not.toContain('Manage Project Listings');
  });
});
