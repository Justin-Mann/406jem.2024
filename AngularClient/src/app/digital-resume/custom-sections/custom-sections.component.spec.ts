import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CustomSectionsComponent } from './custom-sections.component';

describe('CustomSectionsComponent', () => {
  let component: CustomSectionsComponent;
  let fixture: ComponentFixture<CustomSectionsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CustomSectionsComponent]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(CustomSectionsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
