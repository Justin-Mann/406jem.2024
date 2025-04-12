import { Component, inject, AfterViewInit, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SpinnerComponent } from '../spinner/spinner.component';
import { ResumeDataService } from '../services/data/resume-data.service';
import { ResumeData, WorkExperienceItem } from '../interfaces/resume.interface'; // Adjust the path as needed
import { ContactSectionComponent } from './contact-section/contact-section.component';
import { EducationSectionComponent } from "./education-section/education-section.component";
import { CustomSectionsComponent } from "./custom-sections/custom-sections.component";
import { GeneralSectionComponent } from "./general-section/general-section.component";
import { WorkExperienceSectionComponent } from "./work-experience-section/work-experience-section.component";

@Component({
  selector: 'app-digital-resume',
  standalone: true,
  imports: [
    CommonModule,
    SpinnerComponent,
    ContactSectionComponent,
    EducationSectionComponent,
    CustomSectionsComponent,
    GeneralSectionComponent,
    WorkExperienceSectionComponent
],
  templateUrl: './digital-resume.component.html',
  styleUrl: './digital-resume.component.css'
})
export class DigitalResumeComponent implements AfterViewInit{
  title = 'Digital Resume - 406JEM Angular Client';
   _rDataService = inject(ResumeDataService);
   resumes: ResumeData[] | undefined;
   resumeData: ResumeData | undefined;
   resumeLogoUrl = 'assets/img/bojack-samuri_82x100_fl.png';
   @ViewChild(SpinnerComponent) spinnerComponent?: SpinnerComponent;

  ngAfterViewInit(): void {
    this.spinnerComponent?.showSpinner(true);
    this._rDataService.fetchResumeData().subscribe(
      (data) => {
        this.resumeData = data;
        this.spinnerComponent?.showSpinner(false);
      }, 
      (error) => {
        console.error('Error fetching resume data:', error);
        this.spinnerComponent?.showSpinner(false);
      }
    );
  }
}
