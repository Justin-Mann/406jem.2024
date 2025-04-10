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
    Contact: ContactItem[];
    Education: EducationItem[];
    WorkExperience: WorkExperienceItem[];
    CustomSections: CustomSections[];
  }

interface ContactItem {
    type: ContactTypeEnum;
    displayValue: string;
    url?: string;
    mailTo?: string;
  }

interface EducationItem {
    name: string;
    degree: boolean;
    degreeName?: string;
    degreeYear?: string;
    areasOfStudy?: string[];
    endDate?: string;
    gpa?: number;
  }

interface WorkExperienceItem {
    companyName: string;
    position: string;
    startDate: string;
    endDate?: string;
    bulletList?: string[];
    note?: string;
  }

interface CustomSections {
    name: string;
    customItems: CustomSectionItem[];
  }

interface CustomSectionItem {
    value: string;
    type: CustomTypeEnum;
  }

enum CustomTypeEnum {
    Lang, 
    Win, 
    Comp, 
    CompNetwork, 
    Cloud, 
    RDB, 
    DDB, 
    DataLang
  }

enum ContactTypeEnum {
    Phone, 
    Website, 
    Email
  }