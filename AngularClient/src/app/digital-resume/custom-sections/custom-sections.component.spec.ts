import { ComponentRef } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { CustomSectionsComponent } from './custom-sections.component';
import { CustomSections, CustomTypeEnum } from '../../interfaces/resume.interface';

const mockSections: CustomSections[] = [
  {
    name: 'Languages',
    customItems: [
      { value: 'C#', type: CustomTypeEnum.Lang },
      { value: 'TypeScript', type: CustomTypeEnum.Lang }
    ]
  },
  {
    name: 'Cloud',
    customItems: [{ value: 'Azure', type: CustomTypeEnum.Cloud }]
  }
];

describe('CustomSectionsComponent', () => {
  let component: CustomSectionsComponent;
  let fixture: ComponentFixture<CustomSectionsComponent>;
  let ref: ComponentRef<CustomSectionsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CustomSectionsComponent]
    }).compileComponents();

    fixture = TestBed.createComponent(CustomSectionsComponent);
    component = fixture.componentInstance;
    ref = fixture.componentRef;
  });

  it('should create', () => {
    fixture.detectChanges();
    expect(component).toBeTruthy();
  });

  it('should render nothing when customItems is undefined', () => {
    fixture.detectChanges();
    expect((fixture.nativeElement as HTMLElement).textContent?.trim()).toBe('');
  });

  it('should render section names', () => {
    ref.setInput('customItems', mockSections);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Languages');
    expect(compiled.textContent).toContain('Cloud');
  });

  it('should render custom items', () => {
    ref.setInput('customItems', mockSections);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('C#');
    expect(compiled.textContent).toContain('TypeScript');
    expect(compiled.textContent).toContain('Azure');
  });
});
