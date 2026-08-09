import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { HeaderComponent } from './header.component';
import { AuthService } from '../services/auth/auth.service';

describe('HeaderComponent', () => {
  let component: HeaderComponent;
  let fixture: ComponentFixture<HeaderComponent>;
  let authServiceSpy: jasmine.SpyObj<AuthService>;

  beforeEach(async () => {
    authServiceSpy = jasmine.createSpyObj('AuthService', ['isAuthenticated', 'username', 'logout']);
    authServiceSpy.isAuthenticated.and.returnValue(false);
    authServiceSpy.username.and.returnValue(null);

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
});
