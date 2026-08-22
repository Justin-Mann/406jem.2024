/** Mirrors #69's GitHubActivitySettingsDto (GET /api/github-activity-settings/public). A 404
 * means the configured owner has the feature disabled or hasn't configured it at all - callers
 * should treat that as "render nothing", not an error. */
export interface GitHubActivitySettings {
  enabled: boolean;
  gitHubUsername: string | null;
  repoCount: number;
  pinnedRepoNames: string[];
}

/** Subset of GitHub's public, unauthenticated repos API response
 * (https://api.github.com/users/{username}/repos) actually used by the GitHub Activity card -
 * field names are snake_case on the wire, unlike the rest of this app's API. */
export interface GitHubRepo {
  name: string;
  html_url: string;
  description: string | null;
  language: string | null;
  stargazers_count: number;
  fork: boolean;
  pushed_at: string;
}
