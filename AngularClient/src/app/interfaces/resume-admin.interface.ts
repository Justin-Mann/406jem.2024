// Mirrors ResumeFunctions.Models.DigitalResumeModel's wire shape (#31) - a separate, purely
// numeric-enum set of interfaces from resume.interface.ts's display-only ContactTypeEnum/
// CustomTypeEnum, since those don't match the server's actual (non-string-converted) enum wire
// format and this admin editor round-trips Create/Update payloads, not just renders them.

export enum AdminContactTypeEnum {
  Phone = 0,
  Website = 1,
  Email = 2,
}

export enum AdminCustomTypeEnum {
  Lang = 0,
  Win = 1,
  Comp = 2,
  CompNetwork = 3,
  Cloud = 4,
  RDB = 5,
  DDB = 6,
  DataLang = 7,
}

export interface AdminContactItem {
  type: AdminContactTypeEnum | null;
  displayValue?: string | null;
  url?: string | null;
  mailTo?: string | null;
}

export interface AdminEducationItem {
  name?: string | null;
  degree: boolean;
  degreeName?: string | null;
  degreeYear?: string | null;
  areasOfStudy: string[];
}

export interface AdminWorkExperienceItem {
  companyName?: string | null;
  position?: string | null;
  startDate?: string | null;
  endDate?: string | null;
  bulletList: string[];
  note?: string | null;
}

export interface AdminCustomItem {
  value?: string | null;
  type: AdminCustomTypeEnum | null;
}

export interface AdminCustomSectionItem {
  name?: string | null;
  customItems: AdminCustomItem[];
}

export interface AdminSkillItem {
  name?: string | null;
  value?: number | null;
}

export interface AdminSkillAssessmentItem {
  assessorName?: string | null;
  skills: AdminSkillItem[];
}

export interface AdminDigitalResumePayload {
  id?: string | null;
  fName?: string | null;
  mName?: string | null;
  lName?: string | null;
  position?: string | null;
  subtitle?: string | null;
  simpleGoal?: string | null;
  logoFile?: string | null;
  workExperience: AdminWorkExperienceItem[];
  profile: string[];
  contact: AdminContactItem[];
  education: AdminEducationItem[];
  customSections: AdminCustomSectionItem[];
  skillAssessments: AdminSkillAssessmentItem[];
}

export function emptyResumePayload(): AdminDigitalResumePayload {
  return {
    workExperience: [],
    profile: [],
    contact: [],
    education: [],
    customSections: [],
    skillAssessments: [],
  };
}

export interface ResumeDto {
  id: string;
  ownerUserId: string;
  isFeatured: boolean;
  payload: AdminDigitalResumePayload | null;
  createdAtUtc: string;
  updatedAtUtc: string;
  status: string;
  originalFileName?: string | null;
  contentType?: string | null;
  fileSizeBytes?: number | null;
}

export interface CreateOrUpdateResumeRequest {
  ownerUserId?: string | null;
  isFeatured: boolean;
  payload: AdminDigitalResumePayload;
}

export interface ParseResumeResponse {
  resume: ResumeDto;
  parseSucceeded: boolean;
  message?: string | null;
}
