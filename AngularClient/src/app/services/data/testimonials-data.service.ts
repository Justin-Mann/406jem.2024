import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { Testimonial } from '../../interfaces/auth.interface';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class TestimonialsDataService {
  private http = inject(HttpClient);
  private readonly apiBaseUrl = environment.apiBaseUrl;

  list(): Observable<Testimonial[]> {
    return this.http.get<Testimonial[]>(`${this.apiBaseUrl}/api/testimonials`);
  }

  create(message: string): Observable<Testimonial> {
    return this.http.post<Testimonial>(`${this.apiBaseUrl}/api/testimonials`, { message });
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiBaseUrl}/api/testimonials/${id}`);
  }
}
