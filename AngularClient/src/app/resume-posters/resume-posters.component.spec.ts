import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { ResumePostersComponent } from './resume-posters.component';
import { ResumePostersDataService } from '../services/data/resume-posters-data.service';
import { ResumePoster } from '../interfaces/auth.interface';

const posters: ResumePoster[] = [
  { id: 'jane', displayName: 'Jane Doe' },
  { id: 'john', displayName: 'John Smith' }
];

describe('ResumePostersComponent', () => {
  let component: ResumePostersComponent;
  let fixture: ComponentFixture<ResumePostersComponent>;
  let dataServiceSpy: jasmine.SpyObj<ResumePostersDataService>;

  beforeEach(async () => {
    dataServiceSpy = jasmine.createSpyObj('ResumePostersDataService', ['list', 'contact']);
    dataServiceSpy.list.and.returnValue(of(posters));

    await TestBed.configureTestingModule({
      imports: [ResumePostersComponent],
      providers: [
        { provide: ResumePostersDataService, useValue: dataServiceSpy }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(ResumePostersComponent);
    component = fixture.componentInstance;
  });

  it('should create and load resume posters publicly', () => {
    fixture.detectChanges();

    expect(component).toBeTruthy();
    expect(dataServiceSpy.list).toHaveBeenCalledTimes(1);
    expect(component.posters()).toEqual(posters);
  });

  it('does not show a contact form until a poster is clicked', () => {
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;

    expect(compiled.querySelector('textarea')).toBeNull();
  });

  it('shows a click/tap-accessible contact form for a poster when toggled', () => {
    fixture.detectChanges();

    component.toggleForm('jane');
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;

    expect(compiled.querySelector('textarea')).not.toBeNull();
    expect(compiled.querySelector('input[type="email"]')).not.toBeNull();
  });

  it('closes the form when the same poster is toggled again', () => {
    fixture.detectChanges();

    component.toggleForm('jane');
    expect(component.openPosterId()).toBe('jane');

    component.toggleForm('jane');
    expect(component.openPosterId()).toBeNull();
  });

  it('sends the contact request and shows a success message', () => {
    dataServiceSpy.contact.and.returnValue(of(undefined));
    fixture.detectChanges();

    component.replyToEmail = 'visitor@example.com';
    component.message = 'Loved your resume!';
    component.onSubmit('jane');

    expect(dataServiceSpy.contact).toHaveBeenCalledWith('jane', {
      message: 'Loved your resume!',
      replyToEmail: 'visitor@example.com'
    });
    expect(component.successMessage()).toBe('Message sent!');
    expect(component.message).toBe('');
  });

  it('shows an error message when the contact request fails', () => {
    dataServiceSpy.contact.and.returnValue(throwError(() => new Error('fail')));
    fixture.detectChanges();

    component.replyToEmail = 'visitor@example.com';
    component.message = 'Loved your resume!';
    component.onSubmit('jane');

    expect(component.errorMessage()).toBe('Could not send your message. Please try again.');
  });

  it('clears the draft message and reply email when switching to a different poster', () => {
    fixture.detectChanges();

    component.toggleForm('jane');
    component.replyToEmail = 'visitor@example.com';
    component.message = 'Draft for Jane';

    component.toggleForm('john');

    expect(component.message).toBe('');
    expect(component.replyToEmail).toBe('');
  });

  it('does not submit when the message or reply email is blank', () => {
    fixture.detectChanges();

    component.replyToEmail = '';
    component.message = '';
    component.onSubmit('jane');

    expect(dataServiceSpy.contact).not.toHaveBeenCalled();
  });
});
