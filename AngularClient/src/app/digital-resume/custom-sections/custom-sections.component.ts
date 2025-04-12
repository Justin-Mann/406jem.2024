import { Component, Input } from '@angular/core';
import { CustomSections } from '../../interfaces/resume.interface';

@Component({
  selector: 'app-custom-sections',
  standalone: true,
  imports: [],
  templateUrl: './custom-sections.component.html',
  styleUrl: './custom-sections.component.css'
})
export class CustomSectionsComponent {
  @Input()
  get customItems() {
    return this._customItems;
  }
  set customItems(value: CustomSections[] | undefined) {
    this._customItems = value;
  }
  private _customItems: CustomSections[] | undefined;
}
