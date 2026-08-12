import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ContactPosterRequest, ResumePoster } from '../../interfaces/auth.interface';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class ResumePostersDataService {
  private http = inject(HttpClient);
  private readonly apiBaseUrl = environment.apiBaseUrl;

  list(): Observable<ResumePoster[]> {
    return this.http.get<ResumePoster[]>(`${this.apiBaseUrl}/api/resume-posters`);
  }

  contact(posterId: string, request: ContactPosterRequest): Observable<void> {
    return this.http.post<void>(`${this.apiBaseUrl}/api/resume-posters/${posterId}/contact`, request);
  }
}
