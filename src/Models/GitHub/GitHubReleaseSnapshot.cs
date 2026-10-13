namespace F1Widget.Models.GitHub;

public sealed record GitHubReleaseSnapshot(string Tag, IReadOnlyDictionary<string, byte[]> Files);