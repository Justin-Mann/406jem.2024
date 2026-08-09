import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { TestimonialsComponent } from './testimonials.component';
import { TestimonialsDataService } from '../services/data/testimonials-data.service';
import { AuthService } from '../services/auth/auth.service';
import { Testimonial } from '../interfaces/auth.interface';

const testimonials: Testimonial[] = [
  { id: '1', authorUsername: 'jane', message: 'Great site!', createdAtUtc: new Date().toISOString() }
];

describe('TestimonialsComponent', () => {
  let component: TestimonialsComponent;
  let fixture: ComponentFixture<TestimonialsComponent>;
  let dataServiceSpy: jasmine.SpyObj<TestimonialsDataService>;
  let authServiceSpy: jasmine.SpyObj<AuthService>;

  beforeEach(async () => {
    dataServiceSpy = jasmine.createSpyObj('TestimonialsDataService', ['list', 'create', 'delete']);
    dataServiceSpy.list.and.returnValue(of(testimonials));
    authServiceSpy = jasmine.createSpyObj('AuthService', ['isAuthenticated', 'isAdmin']);
    authServiceSpy.isAuthenticated.and.returnValue(false);
    authServiceSpy.isAdmin.and.returnValue(false);

    await TestBed.configureTestingModule({
      imports: [TestimonialsComponent],
      providers: [
        provideRouter([]),
        { provide: TestimonialsDataService, useValue: dataServiceSpy },
        { provide: AuthService, useValue: authServiceSpy }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(TestimonialsComponent);
    component = fixture.componentInstance;
  });

  it('should create and load testimonials publicly', () => {
    fixture.detectChanges();

    expect(component).toBeTruthy();
    expect(dataServiceSpy.list).toHaveBeenCalledTimes(1);
    expect(component.testimonials()).toEqual(testimonials);
  });

  it('hides the post form and shows a login prompt when not authenticated', () => {
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;

    expect(compiled.querySelector('textarea')).toBeNull();
    expect(compiled.textContent).toContain('Log in');
  });

  it('shows the post form when authenticated', () => {
    authServiceSpy.isAuthenticated.and.returnValue(true);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;

    expect(compiled.querySelector('textarea')).not.toBeNull();
  });

  it('does not show delete buttons for a non-admin visitor', () => {
    authServiceSpy.isAuthenticated.and.returnValue(true);
    authServiceSpy.isAdmin.and.returnValue(false);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;

    expect(compiled.textContent).not.toContain('Delete');
  });

  it('shows delete buttons for an admin', () => {
    authServiceSpy.isAuthenticated.and.returnValue(true);
    authServiceSpy.isAdmin.and.returnValue(true);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;

    expect(compiled.textContent).toContain('Delete');
  });

  it('reloads the list after posting a testimonial', () => {
    authServiceSpy.isAuthenticated.and.returnValue(true);
    dataServiceSpy.create.and.returnValue(of(testimonials[0]));
    fixture.detectChanges();

    component.newMessage = 'Nice work!';
    component.onSubmit();

    expect(dataServiceSpy.create).toHaveBeenCalledWith('Nice work!');
    expect(dataServiceSpy.list).toHaveBeenCalledTimes(2);
  });

  it('reloads the list after deleting a testimonial', () => {
    authServiceSpy.isAdmin.and.returnValue(true);
    dataServiceSpy.delete.and.returnValue(of(undefined));
    fixture.detectChanges();

    component.onDelete('1');

    expect(dataServiceSpy.delete).toHaveBeenCalledWith('1');
    expect(dataServiceSpy.list).toHaveBeenCalledTimes(2);
  });
});
