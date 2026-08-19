// Wire shape of GET/PUT api/github-activity-settings/mine (#69) - purely storage for what a
// Resume Admin has configured, no GitHub API calls happen client-side either.

export interface GitHubActivitySettingsDto {
  enabled: boolean;
  gitHubUsername: string | null;
  repoCount: number;
  pinnedRepoNames: string[];
}

export interface UpdateGitHubActivitySettingsRequest {
  enabled: boolean;
  gitHubUsername: string | null;
  repoCount: number | null;
  pinnedRepoNames: string[];
}

export function emptyGitHubActivitySettings(): GitHubActivitySettingsDto {
  return {
    enabled: false,
    gitHubUsername: null,
    repoCount: 5,
    pinnedRepoNames: [],
  };
}
