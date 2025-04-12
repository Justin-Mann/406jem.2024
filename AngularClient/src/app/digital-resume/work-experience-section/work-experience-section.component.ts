import { NgFor, NgIf } from '@angular/common';
import { Component, Input } from '@angular/core';
import { WorkExperienceItem } from '../../interfaces/resume.interface';

@Component({
  selector: 'app-work-experience-section',
  standalone: true,
  imports: [NgFor, NgIf],
  templateUrl: './work-experience-section.component.html',
  styleUrl: './work-experience-section.component.css'
})
export class WorkExperienceSectionComponent {
  @Input()
  get xpItems() {
    return this._experienceItems;
  }
  set xpItems(value: WorkExperienceItem[] | undefined) {
    this._experienceItems = value;
  }
  private _experienceItems: WorkExperienceItem[] | undefined;
}