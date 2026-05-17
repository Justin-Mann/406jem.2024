import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { ResumeDataService } from './resume-data.service';
import { ResumeData } from '../../interfaces/resume.interface';
import { environment } from '../../../environments/environment';

const mockResume: ResumeData = {
  id: 1,
  fName: 'Jane',
  mName: 'Q',
  lName: 'Doe',
  position: 'Software Engineer',
  subtitle: 'C#, Azure',
  simpleGoal: 'Build great software.',
  logoFile: '/img/logo.png',
  profile: ['Detail oriented', 'Team player'],
  contact: [],
  education: [],
  workExperience: [],
  customSections: []
};

describe('ResumeDataService', () => {
  let service: ResumeDataService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(ResumeDataService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should call the correct API endpoint', () => {
    service.fetchResumeData().subscribe();

    const req = httpMock.expectOne(`${environment.apiBaseUrl}/api/resumes/myresume`);
    expect(req.request.method).toBe('GET');
    req.flush(mockResume);
  });

  it('should return resume data on success', () => {
    let result: ResumeData | undefined;

    service.fetchResumeData().subscribe(data => result = data);

    const req = httpMock.expectOne(`${environment.apiBaseUrl}/api/resumes/myresume`);
    req.flush(mockResume);

    expect(result).toEqual(mockResume);
    expect(result?.fName).toBe('Jane');
    expect(result?.lName).toBe('Doe');
  });

  it('should propagate HTTP errors', () => {
    let error: any;

    service.fetchResumeData().subscribe({ error: e => error = e });

    const req = httpMock.expectOne(`${environment.apiBaseUrl}/api/resumes/myresume`);
    req.flush('Not Found', { status: 404, statusText: 'Not Found' });

    expect(error).toBeTruthy();
  });
});
