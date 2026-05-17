import { ComponentRef } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ContactSectionComponent } from './contact-section.component';
import { ContactItem, ContactTypeEnum } from '../../interfaces/resume.interface';

const mockContacts: ContactItem[] = [
  { type: ContactTypeEnum.Phone, displayValue: '555-1234' },
  { type: ContactTypeEnum.Email, displayValue: 'jane@example.com', mailTo: 'jane@example.com' },
  { type: ContactTypeEnum.Website, displayValue: 'jane.dev', url: 'https://jane.dev' }
];

describe('ContactSectionComponent', () => {
  let component: ContactSectionComponent;
  let fixture: ComponentFixture<ContactSectionComponent>;
  let ref: ComponentRef<ContactSectionComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ContactSectionComponent]
    }).compileComponents();

    fixture = TestBed.createComponent(ContactSectionComponent);
    component = fixture.componentInstance;
    ref = fixture.componentRef;
  });

  it('should create', () => {
    fixture.detectChanges();
    expect(component).toBeTruthy();
  });

  it('should render nothing when contactItems is undefined', () => {
    fixture.detectChanges();
    expect((fixture.nativeElement as HTMLElement).textContent?.trim()).toBe('');
  });

  it('should render all contact items', () => {
    ref.setInput('contactItems', mockContacts);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('555-1234');
    expect(compiled.textContent).toContain('jane@example.com');
    expect(compiled.textContent).toContain('jane.dev');
  });

  it('should render correct number of contact items', () => {
    ref.setInput('contactItems', mockContacts);
    fixture.detectChanges();

    const items = (fixture.nativeElement as HTMLElement).querySelectorAll('li');
    expect(items.length).toBe(3);
  });
});
