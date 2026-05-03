import { Component, input } from '@angular/core';
import { CustomSections } from '../../interfaces/resume.interface';

@Component({
  selector: 'app-custom-sections',
  standalone: true,
  imports: [],
  templateUrl: './custom-sections.component.html',
  styleUrl: './custom-sections.component.css'
})
export class CustomSectionsComponent {
  customItems = input<CustomSections[]>();
}
