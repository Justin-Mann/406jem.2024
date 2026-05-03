import { Component, input } from '@angular/core';

@Component({
  selector: 'app-general-section',
  standalone: true,
  imports: [],
  templateUrl: './general-section.component.html',
  styleUrl: './general-section.component.css'
})
export class GeneralSectionComponent {
  sectionName = input<string>();
  profileItems = input<string[]>();
}
