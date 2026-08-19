import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { GitHubActivitySettingsDto, UpdateGitHubActivitySettingsRequest } from '../../interfaces/github-activity-settings.interface';

@Injectable({
  providedIn: 'root'
})
export class GitHubActivitySettingsDataService {
  private http = inject(HttpClient);
  private readonly apiBaseUrl = environment.apiBaseUrl;

  getMine(): Observable<GitHubActivitySettingsDto> {
    return this.http.get<GitHubActivitySettingsDto>(`${this.apiBaseUrl}/api/github-activity-settings/mine`);
  }

  updateMine(request: UpdateGitHubActivitySettingsRequest): Observable<GitHubActivitySettingsDto> {
    return this.http.put<GitHubActivitySettingsDto>(`${this.apiBaseUrl}/api/github-activity-settings/mine`, request);
  }
}
