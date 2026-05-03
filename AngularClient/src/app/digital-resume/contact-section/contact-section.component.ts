import { Component, input } from '@angular/core';
import { ContactItem } from '../../interfaces/resume.interface';

@Component({
  selector: 'app-contact-section',
  standalone: true,
  imports: [],
  templateUrl: './contact-section.component.html',
  styleUrl: './contact-section.component.css'
})
export class ContactSectionComponent {
  contactItems = input<ContactItem[]>();
}
