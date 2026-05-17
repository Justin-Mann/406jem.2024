import { ComponentRef } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { WorkExperienceSectionComponent } from './work-experience-section.component';
import { WorkExperienceItem } from '../../interfaces/resume.interface';

const mockXp: WorkExperienceItem[] = [
  {
    companyName: 'Acme Corp',
    position: 'Senior Developer',
    startDate: '2018',
    endDate: 'Present',
    bulletList: ['Built systems', 'Led reviews'],
    note: 'Remote'
  },
  {
    companyName: 'StartupCo',
    position: 'Junior Developer',
    startDate: '2015',
    endDate: '2018'
  }
];

describe('WorkExperienceSectionComponent', () => {
  let component: WorkExperienceSectionComponent;
  let fixture: ComponentFixture<WorkExperienceSectionComponent>;
  let ref: ComponentRef<WorkExperienceSectionComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [WorkExperienceSectionComponent]
    }).compileComponents();

    fixture = TestBed.createComponent(WorkExperienceSectionComponent);
    component = fixture.componentInstance;
    ref = fixture.componentRef;
  });

  it('should create', () => {
    fixture.detectChanges();
    expect(component).toBeTruthy();
  });

  it('should render nothing when xpItems is undefined', () => {
    fixture.detectChanges();
    expect((fixture.nativeElement as HTMLElement).textContent?.trim()).toBe('');
  });

  it('should render all work experience items', () => {
    ref.setInput('xpItems', mockXp);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Acme Corp');
    expect(compiled.textContent).toContain('StartupCo');
  });

  it('should render company name and position', () => {
    ref.setInput('xpItems', mockXp);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Senior Developer');
    expect(compiled.textContent).toContain('2018');
  });

  it('should render bullet list items', () => {
    ref.setInput('xpItems', [mockXp[0]]);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Built systems');
    expect(compiled.textContent).toContain('Led reviews');
  });
});
