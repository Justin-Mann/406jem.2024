export interface ProjectLink {
  label: string;
  url: string;
  description?: string;
}

export interface ProjectSection {
  heading: string;
  lastUpdated?: string;
  links: ProjectLink[];
}

export interface ProjectListing {
  title?: string;
  sections: ProjectSection[];
}

export interface ProjectListingDto {
  id: string;
  ownerUserId: string;
  isFeatured: boolean;
  payload: ProjectListing | null;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface CreateOrUpdateProjectListingRequest {
  ownerUserId?: string | null;
  isFeatured: boolean;
  payload: ProjectListing;
}
