using System.Text.Json.Serialization;

namespace BlazorApp.Models
{
    /// <summary>Subset of GitHub's public, unauthenticated repos API response
    /// (https://api.github.com/users/{username}/repos) actually used by the GitHub Activity
    /// card - field names are snake_case on the wire, unlike the rest of this app's API.</summary>
    public class GitHubRepoModel
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("html_url")]
        public string HtmlUrl { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("language")]
        public string? Language { get; set; }

        [JsonPropertyName("stargazers_count")]
        public int StargazersCount { get; set; }

        [JsonPropertyName("fork")]
        public bool Fork { get; set; }

        [JsonPropertyName("pushed_at")]
        public DateTimeOffset PushedAt { get; set; }
    }
}
