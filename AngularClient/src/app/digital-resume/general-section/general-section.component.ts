import { Component, input, ChangeDetectionStrategy } from '@angular/core';
import { MatCardModule } from '@angular/material/card';

@Component({
  selector: 'app-general-section',
  standalone: true,
  imports: [MatCardModule],
  templateUrl: './general-section.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './general-section.component.css'
})
export class GeneralSectionComponent {
  sectionName = input<string>();
  profileItems = input<string[]>();
}
