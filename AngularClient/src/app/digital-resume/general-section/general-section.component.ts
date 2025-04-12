import { Component, Input } from '@angular/core';
import { NgFor, NgIf } from '@angular/common';

@Component({
  selector: 'app-general-section',
  standalone: true,
  imports: [NgFor, NgIf],
  templateUrl: './general-section.component.html',
  styleUrl: './general-section.component.css'
})
export class GeneralSectionComponent {
  @Input()
  get sectionName() {
    return this._sectionName;
  }
  set sectionName(value) {
    this._sectionName = value;
  }
  private _sectionName: string | undefined;

  @Input()
  get profileItems() {
    return this._profileItems;
  }
  set profileItems(value) {
    this._profileItems = value;
  }
  private _profileItems: string[] | undefined;
}
