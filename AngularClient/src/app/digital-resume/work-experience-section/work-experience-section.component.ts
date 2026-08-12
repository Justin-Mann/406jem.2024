import { Component, input, ChangeDetectionStrategy } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { WorkExperienceItem } from '../../interfaces/resume.interface';

@Component({
  selector: 'app-work-experience-section',
  standalone: true,
  imports: [MatCardModule],
  templateUrl: './work-experience-section.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './work-experience-section.component.css'
})
export class WorkExperienceSectionComponent {
  xpItems = input<WorkExperienceItem[]>();
}