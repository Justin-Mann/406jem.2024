import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { ManageResumesComponent } from './manage-resumes.component';
import { ResumeAdminDataService } from '../services/data/resume-admin-data.service';
import { AuthService } from '../services/auth/auth.service';
import { AdminContactTypeEnum, ResumeDto } from '../interfaces/resume-admin.interface';

const myResumes: ResumeDto[] = [
  {
    id: 'resume-1',
    ownerUserId: 'admin',
    isFeatured: true,
    payload: { fName: 'Jane', lName: 'Doe', position: 'Engineer', workExperience: [], profile: [], contact: [], education: [], customSections: [], skillAssessments: [] },
    createdAtUtc: '',
    updatedAtUtc: '',
    status: 'Published',
  },
  {
    id: 'resume-2',
    ownerUserId: 'admin',
    isFeatured: false,
    payload: null,
    createdAtUtc: '',
    updatedAtUtc: '',
    status: 'Draft',
    originalFileName: 'resume.pdf',
  },
];

describe('ManageResumesComponent', () => {
  let component: ManageResumesComponent;
  let fixture: ComponentFixture<ManageResumesComponent>;
  let dataServiceSpy: jasmine.SpyObj<ResumeAdminDataService>;
  let authServiceSpy: jasmine.SpyObj<AuthService>;

  beforeEach(async () => {
    dataServiceSpy = jasmine.createSpyObj('ResumeAdminDataService', ['listMine', 'create', 'update', 'delete', 'publish', 'parse', 'upload']);
    dataServiceSpy.listMine.and.returnValue(of(myResumes));
    authServiceSpy = jasmine.createSpyObj('AuthService', ['isAuthenticated', 'isAdmin', 'isSuperAdmin']);
    authServiceSpy.isAdmin.and.returnValue(true);

    await TestBed.configureTestingModule({
      imports: [ManageResumesComponent],
      providers: [
        { provide: ResumeAdminDataService, useValue: dataServiceSpy },
        { provide: AuthService, useValue: authServiceSpy }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(ManageResumesComponent);
    component = fixture.componentInstance;
  });

  it('should create and load the admin\'s resumes', () => {
    fixture.detectChanges();

    expect(component).toBeTruthy();
    expect(dataServiceSpy.listMine).toHaveBeenCalledTimes(1);
    expect(component.resumes()).toEqual(myResumes);
  });

  it('shows an access-required message for a non-admin visitor', () => {
    authServiceSpy.isAdmin.and.returnValue(false);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;

    expect(compiled.textContent).toContain('Resume Admin access required');
  });

  it('renders resume rows with status and featured badges', () => {
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;

    expect(compiled.textContent).toContain('Jane Doe');
    expect(compiled.textContent).toContain('Draft');
    expect(compiled.textContent).toContain('Published');
    expect(compiled.textContent).toContain('Featured');
  });

  it('starts a new resume with an empty payload', () => {
    component.startNew();

    const state = component.editState();
    expect(state?.id).toBeNull();
    expect(state?.payload.profile).toEqual([]);
    expect(state?.payload.contact).toEqual([]);
  });

  it('blocks save and shows a validation error when required fields are missing', () => {
    component.startNew();

    component.save();

    expect(component.validationError()).toContain('required');
    expect(dataServiceSpy.create).not.toHaveBeenCalled();
  });

  it('creates a resume and reloads on save when required fields are present', () => {
    dataServiceSpy.create.and.returnValue(of(myResumes[0]));
    component.startNew();
    const state = component.editState()!;
    state.payload.fName = 'Jane';
    state.payload.lName = 'Doe';
    state.payload.position = 'Engineer';
    component.editState.set({ ...state });

    component.save();

    expect(dataServiceSpy.create).toHaveBeenCalledTimes(1);
    expect(component.editState()).toBeNull();
    expect(dataServiceSpy.listMine).toHaveBeenCalled();
  });

  it('updates an existing resume on save', () => {
    dataServiceSpy.update.and.returnValue(of(myResumes[0]));
    component.startEdit(myResumes[0]);

    component.save();

    expect(dataServiceSpy.update).toHaveBeenCalledWith('resume-1', jasmine.objectContaining({ isFeatured: true }));
  });

  it('shows an error message when saving fails', () => {
    dataServiceSpy.create.and.returnValue(throwError(() => new Error('boom')));
    component.startNew();
    const state = component.editState()!;
    state.payload.fName = 'Jane';
    state.payload.lName = 'Doe';
    state.payload.position = 'Engineer';
    component.editState.set({ ...state });

    component.save();

    expect(component.errorMessage()).toContain('Could not save');
    expect(component.editState()).not.toBeNull();
  });

  it('publishes a draft resume', () => {
    dataServiceSpy.publish.and.returnValue(of(myResumes[1]));

    component.publish('resume-2');

    expect(dataServiceSpy.publish).toHaveBeenCalledWith('resume-2');
    expect(dataServiceSpy.listMine).toHaveBeenCalled();
  });

  it('deletes a resume and reloads', () => {
    dataServiceSpy.delete.and.returnValue(of(undefined));

    component.deleteResume('resume-1');

    expect(dataServiceSpy.delete).toHaveBeenCalledWith('resume-1');
    expect(dataServiceSpy.listMine).toHaveBeenCalled();
  });

  it('parses a resume and opens the edit form with the result', () => {
    const parsed: ResumeDto = { ...myResumes[1], status: 'Draft', payload: { fName: 'Parsed', lName: 'Person', position: 'Dev', workExperience: [], profile: [], contact: [], education: [], customSections: [], skillAssessments: [] } };
    dataServiceSpy.parse.and.returnValue(of({ resume: parsed, parseSucceeded: true, message: null }));

    component.parse('resume-2');

    expect(dataServiceSpy.parse).toHaveBeenCalledWith('resume-2');
    expect(component.editState()?.payload.fName).toBe('Parsed');
    expect(component.statusMessage()).toContain('Parsed successfully');
  });

  it('adds and removes a profile bullet', () => {
    component.startNew();
    component.addProfileBullet();
    expect(component.editState()!.payload.profile.length).toBe(1);

    component.removeProfileBullet(0);
    expect(component.editState()!.payload.profile.length).toBe(0);
  });

  it('adds and removes a work experience entry with a bullet', () => {
    component.startNew();
    component.addWorkExperience();
    const item = component.editState()!.payload.workExperience[0];
    component.addBullet(item);
    expect(component.editState()!.payload.workExperience[0].bulletList.length).toBe(1);

    component.removeWorkExperience(0);
    expect(component.editState()!.payload.workExperience.length).toBe(0);
  });

  it('adds a contact entry with a selectable type', () => {
    component.startNew();
    component.addContact();
    const contact = component.editState()!.payload.contact[0];
    contact.type = AdminContactTypeEnum.Email;

    expect(component.editState()!.payload.contact.length).toBe(1);
    expect(contact.type).toBe(AdminContactTypeEnum.Email);
  });
});
