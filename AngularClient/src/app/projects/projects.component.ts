import { Component, ChangeDetectionStrategy } from '@angular/core';

@Component({
  selector: 'app-projects',
  standalone: true,
  imports: [],
  templateUrl: './projects.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './projects.component.css'
})
export class ProjectsComponent {
  title = 'Projects - 406JEM Angular Client';
}
