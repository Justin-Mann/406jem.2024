import { Component, input } from '@angular/core';
import { WorkExperienceItem } from '../../interfaces/resume.interface';

@Component({
  selector: 'app-work-experience-section',
  standalone: true,
  imports: [],
  templateUrl: './work-experience-section.component.html',
  styleUrl: './work-experience-section.component.css'
})
export class WorkExperienceSectionComponent {
  xpItems = input<WorkExperienceItem[]>();
}