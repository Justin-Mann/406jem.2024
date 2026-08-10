import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { of } from 'rxjs';
import { LoginComponent } from './login.component';
import { AuthService } from '../../services/auth/auth.service';

describe('LoginComponent', () => {
  let component: LoginComponent;
  let fixture: ComponentFixture<LoginComponent>;
  let authServiceSpy: jasmine.SpyObj<AuthService>;
  let router: Router;

  beforeEach(async () => {
    authServiceSpy = jasmine.createSpyObj('AuthService', ['login']);

    await TestBed.configureTestingModule({
      imports: [LoginComponent],
      providers: [provideRouter([]), { provide: AuthService, useValue: authServiceSpy }]
    }).compileComponents();

    fixture = TestBed.createComponent(LoginComponent);
    component = fixture.componentInstance;
    router = TestBed.inject(Router);
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('navigates to /testimonials on successful login', () => {
    spyOn(router, 'navigateByUrl');
    authServiceSpy.login.and.returnValue(of(null));
    component.username = 'jane';
    component.password = 'password123';

    component.onSubmit();

    expect(authServiceSpy.login).toHaveBeenCalledWith({ username: 'jane', password: 'password123' });
    expect(router.navigateByUrl).toHaveBeenCalledWith('/testimonials');
  });

  it('shows the error message and does not navigate on failed login', () => {
    spyOn(router, 'navigateByUrl');
    authServiceSpy.login.and.returnValue(of('Invalid username or password.'));

    component.onSubmit();

    expect(component.errorMessage()).toBe('Invalid username or password.');
    expect(router.navigateByUrl).not.toHaveBeenCalled();
  });
});
