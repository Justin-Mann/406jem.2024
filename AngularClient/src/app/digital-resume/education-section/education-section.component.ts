import { Component, input } from '@angular/core';
import { EducationItem } from '../../interfaces/resume.interface';

@Component({
  selector: 'app-education-section',
  standalone: true,
  imports: [],
  templateUrl: './education-section.component.html',
  styleUrl: './education-section.component.css'
})
export class EducationSectionComponent {
  eduItems = input<EducationItem[]>();
}
