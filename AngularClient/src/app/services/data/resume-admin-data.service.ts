import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { CreateOrUpdateResumeRequest, ParseResumeResponse, ResumeDto } from '../../interfaces/resume-admin.interface';

@Injectable({
  providedIn: 'root'
})
export class ResumeAdminDataService {
  private http = inject(HttpClient);
  private readonly apiBaseUrl = environment.apiBaseUrl;

  listMine(): Observable<ResumeDto[]> {
    return this.http.get<ResumeDto[]>(`${this.apiBaseUrl}/api/resumes/mine`);
  }

  create(request: CreateOrUpdateResumeRequest): Observable<ResumeDto> {
    return this.http.post<ResumeDto>(`${this.apiBaseUrl}/api/resumes`, request);
  }

  update(id: string, request: CreateOrUpdateResumeRequest): Observable<ResumeDto> {
    return this.http.put<ResumeDto>(`${this.apiBaseUrl}/api/resumes/${id}`, request);
  }

  publish(id: string): Observable<ResumeDto> {
    return this.http.post<ResumeDto>(`${this.apiBaseUrl}/api/resumes/${id}/publish`, null);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiBaseUrl}/api/resumes/${id}`);
  }

  parse(id: string): Observable<ParseResumeResponse> {
    return this.http.post<ParseResumeResponse>(`${this.apiBaseUrl}/api/resumes/${id}/parse`, null);
  }

  upload(file: File): Observable<ResumeDto> {
    return this.http.post<ResumeDto>(`${this.apiBaseUrl}/api/resumes/upload`, file, {
      headers: {
        'Content-Type': 'application/pdf',
        'X-File-Name': file.name,
      },
    });
  }
}
