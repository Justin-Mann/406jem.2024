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

  constructor() { }

  // TODO:: this is dumb write a real api that just serves up the resume data

  fetchResumesData(): Observable<ResumeData[]> {
    return this.http.get<ResumeData[]>('assets/static_data/JustinMann_062024.json');
  }

  fetchResumeData(): Observable<ResumeData> {
    var resumes = this.http.get<ResumeData[]>('assets/static_data/JustinMann_062024.json');
    return resumes.pipe(
      filter(resumes => resumes.length > 0),
      map(resumes => resumes[0])
    );
  }
}