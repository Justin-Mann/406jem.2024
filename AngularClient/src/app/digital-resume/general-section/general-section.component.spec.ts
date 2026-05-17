import { ComponentRef } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { GeneralSectionComponent } from './general-section.component';

describe('GeneralSectionComponent', () => {
  let component: GeneralSectionComponent;
  let fixture: ComponentFixture<GeneralSectionComponent>;
  let ref: ComponentRef<GeneralSectionComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [GeneralSectionComponent]
    }).compileComponents();

    fixture = TestBed.createComponent(GeneralSectionComponent);
    component = fixture.componentInstance;
    ref = fixture.componentRef;
  });

  it('should create', () => {
    fixture.detectChanges();
    expect(component).toBeTruthy();
  });

  it('should render section name', () => {
    ref.setInput('sectionName', 'Profile');
    ref.setInput('profileItems', ['Item 1']);
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Profile');
  });

  it('should render all profile items', () => {
    ref.setInput('sectionName', 'Profile');
    ref.setInput('profileItems', ['Detail oriented', 'Team player', 'Problem solver']);
    fixture.detectChanges();

    const items = (fixture.nativeElement as HTMLElement).querySelectorAll('li');
    expect(items.length).toBe(3);
    expect(items[0].textContent).toContain('Detail oriented');
    expect(items[2].textContent).toContain('Problem solver');
  });

  it('should render nothing when inputs are undefined', () => {
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).textContent?.trim()).toBe('');
  });
});
