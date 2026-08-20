namespace BlazorApp.Models
{
    /// <summary>Wire shape of GET/PUT api/github-activity-settings/mine (#69) - purely storage
    /// for what a Resume Admin has configured, no GitHub API calls happen client-side either.</summary>
    public class GitHubActivitySettingsDto
    {
        public bool Enabled { get; set; }
        public string? GitHubUsername { get; set; }
        public int RepoCount { get; set; }
        public List<string> PinnedRepoNames { get; set; } = new();
    }

    public class UpdateGitHubActivitySettingsRequest
    {
        public bool Enabled { get; set; }
        public string? GitHubUsername { get; set; }
        public int? RepoCount { get; set; }
        public List<string> PinnedRepoNames { get; set; } = new();
    }
}
