import { HttpClient } from '@angular/common/http';
import { inject, Injectable,  Input } from '@angular/core';
import { filter, map, Observable } from 'rxjs';
import { ResumeData } from '../../interfaces/resume.interface';

@Injectable({
  providedIn: 'root'
})
export class ResumeDataService {
  @Input() resumeId!: number;
  private http = inject(HttpClient);
  private apiBaseUrl = 'https://406jem-resume-api.azurewebsites.net';

  constructor() { }

  fetchResumesData(): Observable<ResumeData[]> {
    return this.http.get<ResumeData[]>(this.apiBaseUrl + '/api/resumes');
  }

  fetchResumeData(): Observable<ResumeData> {
    return this.http.get<ResumeData>(this.apiBaseUrl + '/api/myResume');
  }
}
