import { Component, inject, AfterViewInit, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SpinnerComponent } from '../spinner/spinner.component';
import { ResumeDataService } from '../services/data/resume-data.service';
import { ResumeData } from '../interfaces/resume.interface'; // Adjust the path as needed

@Component({
  selector: 'app-digital-resume',
  standalone: true,
  imports: [SpinnerComponent, CommonModule],
  templateUrl: './digital-resume.component.html',
  styleUrl: './digital-resume.component.css'
})
export class DigitalResumeComponent implements AfterViewInit{
  title = 'Digital Resume - 406JEM Angular Client';
   _rDataService = inject(ResumeDataService);
   resumes: ResumeData[] | undefined;
   resumeData: ResumeData | undefined;
   @ViewChild(SpinnerComponent) spinnerComponent?: SpinnerComponent;

  ngAfterViewInit(): void {
    this.spinnerComponent?.showSpinner(true);
    this._rDataService.fetchResumeData().subscribe(
      (data) => {
        this.resumeData = data;
        alert('Data loaded successfully!');
        this.spinnerComponent?.showSpinner(false);
      }, 
      (error) => {
        console.error('Error fetching resume data:', error);
        this.spinnerComponent?.showSpinner(false);
      }
    );
  }
}
