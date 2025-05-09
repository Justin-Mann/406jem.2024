import { ComponentFixture, TestBed } from '@angular/core/testing';

import { DigitalResumeComponent } from './digital-resume.component';

describe('DigitalResumeComponent', () => {
  let component: DigitalResumeComponent;
  let fixture: ComponentFixture<DigitalResumeComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DigitalResumeComponent]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(DigitalResumeComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
