import { ComponentRef } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { EducationSectionComponent } from './education-section.component';
import { EducationItem } from '../../interfaces/resume.interface';

const mockEducation: EducationItem[] = [
  {
    name: 'State University',
    degree: true,
    degreeName: 'B.S. Computer Science',
    degreeYear: '2015',
    areasOfStudy: ['Algorithms', 'Systems']
  }
];

describe('EducationSectionComponent', () => {
  let component: EducationSectionComponent;
  let fixture: ComponentFixture<EducationSectionComponent>;
  let ref: ComponentRef<EducationSectionComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [EducationSectionComponent]
    }).compileComponents();

    fixture = TestBed.createComponent(EducationSectionComponent);
    component = fixture.componentInstance;
    ref = fixture.componentRef;
  });

  it('should create', () => {
    fixture.detectChanges();
    expect(component).toBeTruthy();
  });

  it('should render nothing when eduItems is undefined', () => {
    fixture.detectChanges();
    expect((fixture.nativeElement as HTMLElement).textContent?.trim()).toBe('');
  });

  it('should render education entries', () => {
    ref.setInput('eduItems', mockEducation);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('State University');
    expect(compiled.textContent).toContain('B.S. Computer Science');
  });
});
