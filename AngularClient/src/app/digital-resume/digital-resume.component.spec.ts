import { ComponentFixture, TestBed } from '@angular/core/testing';
import { DigitalResumeComponent } from './digital-resume.component';
import { ResumeDataService } from '../services/data/resume-data.service';
import { ResumeData } from '../interfaces/resume.interface';
import { of, throwError } from 'rxjs';

const mockResume: ResumeData = {
  id: 1,
  fName: 'Jane',
  mName: 'Q',
  lName: 'Doe',
  position: 'Software Engineer',
  subtitle: 'C#, Azure',
  simpleGoal: 'Build great software.',
  logoFile: '/img/logo.png',
  profile: ['Detail oriented', 'Team player'],
  contact: [],
  education: [],
  workExperience: [],
  customSections: []
};

describe('DigitalResumeComponent', () => {
  let component: DigitalResumeComponent;
  let fixture: ComponentFixture<DigitalResumeComponent>;
  let dataServiceSpy: jasmine.SpyObj<ResumeDataService>;

  beforeEach(async () => {
    dataServiceSpy = jasmine.createSpyObj('ResumeDataService', ['fetchResumeData']);
    dataServiceSpy.fetchResumeData.and.returnValue(of(mockResume));

    await TestBed.configureTestingModule({
      imports: [DigitalResumeComponent],
      providers: [{ provide: ResumeDataService, useValue: dataServiceSpy }]
    }).compileComponents();

    fixture = TestBed.createComponent(DigitalResumeComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    fixture.detectChanges();
    expect(component).toBeTruthy();
  });

  it('should start in loading state', () => {
    expect(component.isLoading()).toBeTrue();
    expect(component.resumeData()).toBeNull();
  });

  it('should load resume data on init', () => {
    fixture.detectChanges();

    expect(dataServiceSpy.fetchResumeData).toHaveBeenCalledTimes(1);
    expect(component.resumeData()).toEqual(mockResume);
    expect(component.isLoading()).toBeFalse();
  });

  it('should display name after load', () => {
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;

    expect(compiled.textContent).toContain('Jane');
    expect(compiled.textContent).toContain('Doe');
  });

  it('should display position after load', () => {
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;

    expect(compiled.textContent).toContain('Software Engineer');
  });

  it('should stop loading on error', () => {
    dataServiceSpy.fetchResumeData.and.returnValue(throwError(() => new Error('Network error')));

    fixture.detectChanges();

    expect(component.isLoading()).toBeFalse();
    expect(component.resumeData()).toBeNull();
  });
});
