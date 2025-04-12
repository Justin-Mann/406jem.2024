import { Component, Input } from '@angular/core';
import { ContactItem } from '../../interfaces/resume.interface';

@Component({
  selector: 'app-contact-section',
  standalone: true,
  imports: [],
  templateUrl: './contact-section.component.html',
  styleUrl: './contact-section.component.css'
})
export class ContactSectionComponent {
  @Input()
  get contactItems() {
    return this._contacts;
  }
  set contactItems(value) {
    this._contacts = value;
  }
  private _contacts: ContactItem[] | undefined;
}
