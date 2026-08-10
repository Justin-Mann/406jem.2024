import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { CreateOrUpdateProjectListingRequest, ProjectListing, ProjectListingDto } from '../../interfaces/project-listing.interface';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class ProjectListingsDataService {
  private http = inject(HttpClient);
  private readonly apiBaseUrl = environment.apiBaseUrl;

  getPublic(): Observable<ProjectListing> {
    return this.http.get<ProjectListing>(`${this.apiBaseUrl}/api/projectlistings/public`);
  }

  listMine(): Observable<ProjectListingDto[]> {
    return this.http.get<ProjectListingDto[]>(`${this.apiBaseUrl}/api/projectlistings/mine`);
  }

  create(request: CreateOrUpdateProjectListingRequest): Observable<ProjectListingDto> {
    return this.http.post<ProjectListingDto>(`${this.apiBaseUrl}/api/projectlistings`, request);
  }

  update(id: string, request: CreateOrUpdateProjectListingRequest): Observable<ProjectListingDto> {
    return this.http.put<ProjectListingDto>(`${this.apiBaseUrl}/api/projectlistings/${id}`, request);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiBaseUrl}/api/projectlistings/${id}`);
  }
}
