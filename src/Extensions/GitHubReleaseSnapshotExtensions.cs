using System.Text.Json;
using F1Widget.Models.GitHub;

namespace F1Widget.Extensions;

public static class GitHubReleaseSnapshotExtensions
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static T? GetJson<T>(this GitHubReleaseSnapshot snapshot, string path)
    {
        return snapshot.Files.TryGetValue(path, out var bytes)
            ? JsonSerializer.Deserialize<T>(bytes, Options)
            : default;
    }
}