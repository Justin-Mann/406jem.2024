import { Component, inject, signal, OnInit } from '@angular/core';
import { SpinnerComponent } from '../spinner/spinner.component';
import { ResumeDataService } from '../services/data/resume-data.service';
import { ResumeData } from '../interfaces/resume.interface';
import { ContactSectionComponent } from './contact-section/contact-section.component';
import { EducationSectionComponent } from "./education-section/education-section.component";
import { CustomSectionsComponent } from "./custom-sections/custom-sections.component";
import { GeneralSectionComponent } from "./general-section/general-section.component";
import { WorkExperienceSectionComponent } from "./work-experience-section/work-experience-section.component";

@Component({
  selector: 'app-digital-resume',
  standalone: true,
  imports: [
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
export class DigitalResumeComponent implements OnInit {
  private dataService = inject(ResumeDataService);

  resumeData = signal<ResumeData | null>(null);
  isLoading = signal(true);
  readonly logoUrl = 'assets/img/bojack-samuri_82x100_fl.png';

  ngOnInit(): void {
    this.dataService.fetchResumeData().subscribe({
      next: (data) => {
        this.resumeData.set(data);
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('Error fetching resume data:', err);
        this.isLoading.set(false);
      }
    });
  }
}
