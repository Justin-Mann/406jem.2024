import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ResumeData } from '../../interfaces/resume.interface';

@Injectable({
  providedIn: 'root'
})
export class ResumeDataService {
  private http = inject(HttpClient);
  private readonly apiBaseUrl = 'https://myresumeapi20250620071718.azurewebsites.net';

  fetchResumeData(): Observable<ResumeData> {
    return this.http.get<ResumeData>(`${this.apiBaseUrl}/resumes/myresume`);
  }
}
