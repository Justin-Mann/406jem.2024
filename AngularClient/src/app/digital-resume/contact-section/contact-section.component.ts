import { Component, input, ChangeDetectionStrategy } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { ContactItem } from '../../interfaces/resume.interface';

@Component({
  selector: 'app-contact-section',
  standalone: true,
  imports: [MatCardModule],
  templateUrl: './contact-section.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './contact-section.component.css'
})
export class ContactSectionComponent {
  contactItems = input<ContactItem[]>();
}
