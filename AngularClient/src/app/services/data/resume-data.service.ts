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
  private apiBaseUrl = 'https://myresumeapi20250620071718.azurewebsites.net';
  constructor() { }

  fetchResumesData(): Observable<ResumeData[]> {
    return this.http.get<ResumeData[]>(this.apiBaseUrl + '/resumes/:resumeId');
  }

  fetchResumeData(): Observable<ResumeData> {
    return this.http.get<ResumeData>(this.apiBaseUrl + '/resumes/myresume');
  }
}
