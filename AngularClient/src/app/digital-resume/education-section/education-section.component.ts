import { Component, Input } from '@angular/core';
import { EducationItem } from '../../interfaces/resume.interface';

@Component({
  selector: 'app-education-section',
  standalone: true,
  imports: [],
  templateUrl: './education-section.component.html',
  styleUrl: './education-section.component.css'
})
export class EducationSectionComponent {
  @Input()
  get eduItems() {
    return this._eduItems;
  }
  set eduItems(value: EducationItem[] | undefined) {
    this._eduItems = value;
  }
  private _eduItems: EducationItem[] | undefined;
}
