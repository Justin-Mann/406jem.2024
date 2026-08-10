import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient, withXhr } from '@angular/common/http';
import { ProjectListingsDataService } from './project-listings-data.service';
import { ProjectListing, ProjectListingDto } from '../../interfaces/project-listing.interface';
import { environment } from '../../../environments/environment';

const mockListing: ProjectListing = {
  title: 'Projects',
  sections: [
    { heading: 'WWW', lastUpdated: '04/2025', links: [{ label: 'GitHub', url: 'https://github.com/Justin-Mann' }] }
  ]
};

const mockDto: ProjectListingDto = {
  id: '1',
  ownerUserId: 'admin',
  isFeatured: true,
  payload: mockListing,
  createdAtUtc: new Date().toISOString(),
  updatedAtUtc: new Date().toISOString()
};

describe('ProjectListingsDataService', () => {
  let service: ProjectListingsDataService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(withXhr()), provideHttpClientTesting()]
    });
    service = TestBed.inject(ProjectListingsDataService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('fetches the public listing', () => {
    let result: ProjectListing | undefined;
    service.getPublic().subscribe(l => result = l);

    const req = httpMock.expectOne(`${environment.apiBaseUrl}/api/projectlistings/public`);
    expect(req.request.method).toBe('GET');
    req.flush(mockListing);

    expect(result).toEqual(mockListing);
  });

  it('lists the caller\'s own listings', () => {
    let result: ProjectListingDto[] | undefined;
    service.listMine().subscribe(l => result = l);

    const req = httpMock.expectOne(`${environment.apiBaseUrl}/api/projectlistings/mine`);
    expect(req.request.method).toBe('GET');
    req.flush([mockDto]);

    expect(result).toEqual([mockDto]);
  });

  it('creates a listing', () => {
    service.create({ isFeatured: false, payload: mockListing }).subscribe();

    const req = httpMock.expectOne(`${environment.apiBaseUrl}/api/projectlistings`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ isFeatured: false, payload: mockListing });
    req.flush(mockDto);
  });

  it('updates a listing', () => {
    service.update('1', { isFeatured: true, payload: mockListing }).subscribe();

    const req = httpMock.expectOne(`${environment.apiBaseUrl}/api/projectlistings/1`);
    expect(req.request.method).toBe('PUT');
    req.flush(mockDto);
  });

  it('deletes a listing', () => {
    service.delete('1').subscribe();

    const req = httpMock.expectOne(`${environment.apiBaseUrl}/api/projectlistings/1`);
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
  });
});
