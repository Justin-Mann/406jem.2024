import { Component, input, ChangeDetectionStrategy } from '@angular/core';
import { EducationItem } from '../../interfaces/resume.interface';

@Component({
  selector: 'app-education-section',
  standalone: true,
  imports: [],
  templateUrl: './education-section.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './education-section.component.css'
})
export class EducationSectionComponent {
  eduItems = input<EducationItem[]>();
}
