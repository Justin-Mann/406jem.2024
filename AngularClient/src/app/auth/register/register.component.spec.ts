import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { of } from 'rxjs';
import { RegisterComponent } from './register.component';
import { AuthService } from '../../services/auth/auth.service';

describe('RegisterComponent', () => {
  let component: RegisterComponent;
  let fixture: ComponentFixture<RegisterComponent>;
  let authServiceSpy: jasmine.SpyObj<AuthService>;
  let router: Router;

  beforeEach(async () => {
    authServiceSpy = jasmine.createSpyObj('AuthService', ['register', 'login']);

    await TestBed.configureTestingModule({
      imports: [RegisterComponent],
      providers: [provideRouter([]), { provide: AuthService, useValue: authServiceSpy }]
    }).compileComponents();

    fixture = TestBed.createComponent(RegisterComponent);
    component = fixture.componentInstance;
    router = TestBed.inject(Router);
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('registers then logs in and navigates on success', () => {
    spyOn(router, 'navigateByUrl');
    authServiceSpy.register.and.returnValue(of(null));
    authServiceSpy.login.and.returnValue(of(null));
    component.username = 'jane';
    component.email = 'jane@example.com';
    component.password = 'password123';

    component.onSubmit();

    expect(authServiceSpy.register).toHaveBeenCalledWith({ username: 'jane', email: 'jane@example.com', password: 'password123' });
    expect(authServiceSpy.login).toHaveBeenCalledWith({ username: 'jane', password: 'password123' });
    expect(router.navigateByUrl).toHaveBeenCalledWith('/');
  });

  it('shows the error and does not attempt login when registration fails', () => {
    authServiceSpy.register.and.returnValue(of('That username is already taken.'));

    component.onSubmit();

    expect(component.errorMessage()).toBe('That username is already taken.');
    expect(authServiceSpy.login).not.toHaveBeenCalled();
  });
});
