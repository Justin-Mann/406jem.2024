import { Component, Input } from '@angular/core';
import { ContactItem, ContactTypeEnum } from '../../interfaces/resume.interface';
import { NgFor, NgIf } from '@angular/common';
import { WorkExperienceSectionComponent } from "../work-experience-section/work-experience-section.component";

@Component({
  selector: 'app-contact-section',
  standalone: true,
  imports: [NgFor, NgIf, WorkExperienceSectionComponent],
  templateUrl: './contact-section.component.html',
  styleUrl: './contact-section.component.css'
})
export class ContactSectionComponent {
  contactItemData: ContactItem | undefined;

  @Input()
  get contactItems() {
    return this._contacts;
  }
  set contactItems(value) {
    this._contacts = value;
  }
  private _contacts: ContactItem[] | undefined;
}
