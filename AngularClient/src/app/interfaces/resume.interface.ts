export interface ResumeData {
    id: number;
    fName: string;
    mName: string;
    lName: string;
    position: string;
    subtitle: string;
    simpleGoal: string;
    logoFile: string;
    profile: string[];
    contact: ContactItem[];
    education: EducationItem[];
    workExperience: WorkExperienceItem[];
    customSections: CustomSections[];
  }

export interface ContactItem {
    type: ContactTypeEnum;
    displayValue: string;
    url?: string;
    mailTo?: string;
  }

export interface EducationItem {
    name: string;
    degree: boolean;
    degreeName?: string;
    degreeYear?: string;
    areasOfStudy?: string[];
    endDate?: string;
    gpa?: number;
  }

export interface WorkExperienceItem {
    companyName: string;
    position: string;
    startDate: string;
    endDate?: string;
    bulletList?: string[];
    note?: string;
  }

export interface CustomSections {
    name: string;
    customItems: CustomSectionItem[];
  }

export interface CustomSectionItem {
    value: string;
    type: CustomTypeEnum;
  }

export enum CustomTypeEnum {
    Lang = 'lang', 
    Win = 'win', 
    Comp = 'comp', 
    CompNetwork = 'compNetwork', 
    Cloud = 'cloud', 
    RDB = 'rdb', 
    DDB = 'ddb', 
    DataLang = 'dataLang',
  }

export enum ContactTypeEnum {
    Phone = 0, 
    Website = 1, 
    Email = 2,
  }