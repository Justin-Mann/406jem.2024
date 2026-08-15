import { Component, inject, signal, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatListModule } from '@angular/material/list';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatChipsModule } from '@angular/material/chips';
import { MatDividerModule } from '@angular/material/divider';
import { ResumeAdminDataService } from '../services/data/resume-admin-data.service';
import { AuthService } from '../services/auth/auth.service';
import {
  AdminContactItem,
  AdminContactTypeEnum,
  AdminCustomItem,
  AdminCustomSectionItem,
  AdminCustomTypeEnum,
  AdminDigitalResumePayload,
  AdminEducationItem,
  AdminSkillAssessmentItem,
  AdminSkillItem,
  AdminWorkExperienceItem,
  CreateOrUpdateResumeRequest,
  emptyResumePayload,
  ResumeDto,
} from '../interfaces/resume-admin.interface';

interface ResumeEditState {
  id: string | null;
  isFeatured: boolean;
  payload: AdminDigitalResumePayload;
}

@Component({
  selector: 'app-manage-resumes',
  standalone: true,
  imports: [
    FormsModule,
    MatCardModule,
    MatButtonModule,
    MatListModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatCheckboxModule,
    MatChipsModule,
    MatDividerModule,
  ],
  templateUrl: './manage-resumes.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './manage-resumes.component.css',
})
export class ManageResumesComponent implements OnInit {
  private dataService = inject(ResumeAdminDataService);
  authService = inject(AuthService);

  contactTypes = [
    { value: AdminContactTypeEnum.Phone, label: 'Phone' },
    { value: AdminContactTypeEnum.Website, label: 'Website' },
    { value: AdminContactTypeEnum.Email, label: 'Email' },
  ];

  customTypes = Object.keys(AdminCustomTypeEnum)
    .filter(k => isNaN(Number(k)))
    .map(k => ({ value: AdminCustomTypeEnum[k as keyof typeof AdminCustomTypeEnum], label: k }));

  resumes = signal<ResumeDto[] | null>(null);
  editState = signal<ResumeEditState | null>(null);
  errorMessage = signal<string | null>(null);
  validationError = signal<string | null>(null);
  statusMessage = signal<string | null>(null);
  isBusy = signal(false);
  uploadBusy = signal(false);

  ngOnInit(): void {
    this.loadMine();
  }

  private loadMine(): void {
    this.errorMessage.set(null);
    this.dataService.listMine().subscribe({
      next: items => this.resumes.set(items),
      error: () => {
        this.errorMessage.set('Could not load your resumes.');
        this.resumes.set([]);
      }
    });
  }

  resumeDisplayName(resume: ResumeDto): string {
    const name = [resume.payload?.fName, resume.payload?.lName].filter(Boolean).join(' ');
    return name || '(untitled resume)';
  }

  isDraft(resume: ResumeDto): boolean {
    return resume.status === 'Draft';
  }

  startNew(): void {
    this.validationError.set(null);
    this.editState.set({ id: null, isFeatured: false, payload: emptyResumePayload() });
  }

  startEdit(resume: ResumeDto): void {
    this.validationError.set(null);
    this.editState.set({
      id: resume.id,
      isFeatured: resume.isFeatured,
      payload: this.clonePayload(resume.payload ?? emptyResumePayload()),
    });
  }

  cancelEdit(): void {
    this.editState.set(null);
    this.validationError.set(null);
  }

  save(): void {
    const state = this.editState();
    if (!state) return;

    const payload = state.payload;
    if (!payload.fName?.trim() || !payload.lName?.trim() || !payload.position?.trim()) {
      this.validationError.set('First name, last name, and position are required.');
      return;
    }

    this.validationError.set(null);
    this.isBusy.set(true);
    this.errorMessage.set(null);

    const request: CreateOrUpdateResumeRequest = { isFeatured: state.isFeatured, payload };
    const result$ = state.id ? this.dataService.update(state.id, request) : this.dataService.create(request);

    result$.subscribe({
      next: () => {
        this.isBusy.set(false);
        this.editState.set(null);
        this.statusMessage.set('Resume saved.');
        this.loadMine();
      },
      error: () => {
        this.isBusy.set(false);
        this.errorMessage.set('Could not save this resume. Please try again.');
      }
    });
  }

  deleteResume(id: string): void {
    this.isBusy.set(true);
    this.errorMessage.set(null);

    this.dataService.delete(id).subscribe({
      next: () => {
        this.isBusy.set(false);
        this.loadMine();
      },
      error: () => {
        this.isBusy.set(false);
        this.errorMessage.set('Could not delete this resume. Please try again.');
      }
    });
  }

  publish(id: string): void {
    this.isBusy.set(true);
    this.errorMessage.set(null);

    this.dataService.publish(id).subscribe({
      next: () => {
        this.isBusy.set(false);
        this.statusMessage.set('Resume published.');
        this.loadMine();
      },
      error: () => {
        this.isBusy.set(false);
        this.errorMessage.set('Could not publish this resume. Please try again.');
      }
    });
  }

  parse(id: string): void {
    this.isBusy.set(true);
    this.errorMessage.set(null);
    this.statusMessage.set(null);

    this.dataService.parse(id).subscribe({
      next: result => {
        this.isBusy.set(false);
        this.statusMessage.set(
          result.parseSucceeded
            ? 'Parsed successfully - review the fields below before publishing.'
            : result.message || 'Automatic parsing failed. You can enter the resume details manually.'
        );
        this.loadMine();
        this.startEdit(result.resume);
      },
      error: () => {
        this.isBusy.set(false);
        this.errorMessage.set('Could not parse this resume. Please try again.');
      }
    });
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';
    if (!file) return;

    this.uploadBusy.set(true);
    this.errorMessage.set(null);
    this.statusMessage.set(null);

    this.dataService.upload(file).subscribe({
      next: uploaded => {
        this.uploadBusy.set(false);
        this.statusMessage.set('Uploaded - starting AI parsing...');
        this.loadMine();
        this.parse(uploaded.id);
      },
      error: () => {
        this.uploadBusy.set(false);
        this.errorMessage.set('Could not upload this PDF. Please try again.');
      }
    });
  }

  addProfileBullet(): void {
    this.editState()!.payload.profile.push('');
    this.touch();
  }
  removeProfileBullet(index: number): void {
    this.editState()!.payload.profile.splice(index, 1);
    this.touch();
  }

  addWorkExperience(): void {
    this.editState()!.payload.workExperience.push(this.emptyWorkExperience());
    this.touch();
  }
  removeWorkExperience(index: number): void {
    this.editState()!.payload.workExperience.splice(index, 1);
    this.touch();
  }
  addBullet(item: AdminWorkExperienceItem): void {
    item.bulletList.push('');
    this.touch();
  }
  removeBullet(item: AdminWorkExperienceItem, index: number): void {
    item.bulletList.splice(index, 1);
    this.touch();
  }

  addEducation(): void {
    this.editState()!.payload.education.push(this.emptyEducation());
    this.touch();
  }
  removeEducation(index: number): void {
    this.editState()!.payload.education.splice(index, 1);
    this.touch();
  }
  addAreaOfStudy(item: AdminEducationItem): void {
    item.areasOfStudy.push('');
    this.touch();
  }
  removeAreaOfStudy(item: AdminEducationItem, index: number): void {
    item.areasOfStudy.splice(index, 1);
    this.touch();
  }

  addContact(): void {
    this.editState()!.payload.contact.push(this.emptyContact());
    this.touch();
  }
  removeContact(index: number): void {
    this.editState()!.payload.contact.splice(index, 1);
    this.touch();
  }

  addCustomSection(): void {
    this.editState()!.payload.customSections.push(this.emptyCustomSection());
    this.touch();
  }
  removeCustomSection(index: number): void {
    this.editState()!.payload.customSections.splice(index, 1);
    this.touch();
  }
  addCustomItem(section: AdminCustomSectionItem): void {
    section.customItems.push(this.emptyCustomItem());
    this.touch();
  }
  removeCustomItem(section: AdminCustomSectionItem, index: number): void {
    section.customItems.splice(index, 1);
    this.touch();
  }

  addSkillAssessment(): void {
    this.editState()!.payload.skillAssessments.push(this.emptySkillAssessment());
    this.touch();
  }
  removeSkillAssessment(index: number): void {
    this.editState()!.payload.skillAssessments.splice(index, 1);
    this.touch();
  }
  addSkill(assessment: AdminSkillAssessmentItem): void {
    assessment.skills.push(this.emptySkill());
    this.touch();
  }
  removeSkill(assessment: AdminSkillAssessmentItem, index: number): void {
    assessment.skills.splice(index, 1);
    this.touch();
  }

  private touch(): void {
    const state = this.editState();
    if (state) {
      this.editState.set({ ...state });
    }
  }

  private clonePayload(source: AdminDigitalResumePayload): AdminDigitalResumePayload {
    return {
      id: source.id,
      fName: source.fName,
      mName: source.mName,
      lName: source.lName,
      position: source.position,
      subtitle: source.subtitle,
      simpleGoal: source.simpleGoal,
      logoFile: source.logoFile,
      profile: [...(source.profile ?? [])],
      contact: (source.contact ?? []).map(c => ({ ...c })),
      education: (source.education ?? []).map(e => ({ ...e, areasOfStudy: [...(e.areasOfStudy ?? [])] })),
      workExperience: (source.workExperience ?? []).map(w => ({ ...w, bulletList: [...(w.bulletList ?? [])] })),
      customSections: (source.customSections ?? []).map(cs => ({ ...cs, customItems: (cs.customItems ?? []).map(ci => ({ ...ci })) })),
      skillAssessments: (source.skillAssessments ?? []).map(sa => ({ ...sa, skills: (sa.skills ?? []).map(s => ({ ...s })) })),
    };
  }

  private emptyWorkExperience(): AdminWorkExperienceItem {
    return { bulletList: [] };
  }
  private emptyEducation(): AdminEducationItem {
    return { degree: false, areasOfStudy: [] };
  }
  private emptyContact(): AdminContactItem {
    return { type: null };
  }
  private emptyCustomSection(): AdminCustomSectionItem {
    return { customItems: [] };
  }
  private emptyCustomItem(): AdminCustomItem {
    return { type: null };
  }
  private emptySkillAssessment(): AdminSkillAssessmentItem {
    return { skills: [] };
  }
  private emptySkill(): AdminSkillItem {
    return {};
  }
}
