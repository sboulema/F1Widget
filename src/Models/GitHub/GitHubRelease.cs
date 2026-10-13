using System.Text.Json.Serialization;

namespace F1Widget.Models.GitHub;

public sealed class GitHubRelease
{
    [JsonPropertyName("tag_name")]
    public string? TagName { get; set; }

    [JsonPropertyName("zipball_url")]
    public string ZipballUrl { get; set; } = string.Empty;

    [JsonPropertyName("assets")]
    public List<GitHubAsset> Assets { get; set; } = [];
}