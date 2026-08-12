import { Component, input, ChangeDetectionStrategy } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { CustomSections } from '../../interfaces/resume.interface';

@Component({
  selector: 'app-custom-sections',
  standalone: true,
  imports: [MatCardModule],
  templateUrl: './custom-sections.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './custom-sections.component.css'
})
export class CustomSectionsComponent {
  customItems = input<CustomSections[]>();
}
