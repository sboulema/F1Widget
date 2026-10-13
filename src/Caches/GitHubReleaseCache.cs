using System.IO.Compression;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using F1Widget.Models.GitHub;

namespace F1Widget.Caches;

public sealed class GitHubReleaseCache : IDisposable
{
    private readonly HttpClient _http;
    private readonly string _owner;
    private readonly string _repo;
    private readonly string _releaseAssetName;
    private readonly string _baseDir;

    private GitHubReleaseSnapshot? _current;

    public GitHubReleaseCache(string owner, string repo, string releaseAssetName, string? token = null)
    {
        _owner = owner;
        _repo = repo;
        _releaseAssetName = releaseAssetName;
        _baseDir = Path.Combine(Path.GetTempPath(), "F1Widget", owner, repo);

        _http = new HttpClient();
        _http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("F1Widget", "1.0"));
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

        if (!string.IsNullOrEmpty(token))
        {
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
    }

    /// <summary>Current release, or null if nothing has been loaded yet.</summary>
    public GitHubReleaseSnapshot? Current => Volatile.Read(ref _current);

    /// <summary>Checks GitHub once. Returns true if a new release was loaded.</summary>
    public async Task<bool> CheckForUpdateAsync(CancellationToken ct = default)
    {
        var release = await _http.GetFromJsonAsync<GitHubRelease>(
            $"https://api.github.com/repos/{_owner}/{_repo}/releases/latest", ct);

        if (release?.TagName is null || release.TagName == Current?.Tag)
        {
            return false;
        }

        // Prefer the first .zip asset, fall back to the source zipball
        var downloadUrl = release.Assets
            .FirstOrDefault(a => a.Name.EndsWith(_releaseAssetName, StringComparison.OrdinalIgnoreCase))
            ?.DownloadUrl ?? release.ZipballUrl;

        var targetDir = Path.Combine(_baseDir, release.TagName);

        if (Directory.Exists(targetDir))
        {
            Directory.Delete(targetDir, recursive: true);
        }

        Directory.CreateDirectory(targetDir);

        await using (var zipStream = await _http.GetStreamAsync(downloadUrl, ct))
        {
            await ZipFile.ExtractToDirectoryAsync(zipStream, targetDir, overwriteFiles: true, ct);
        }

        // Load all files into memory
        var files = await Task.WhenAll(
            Directory.EnumerateFiles(targetDir, "*", SearchOption.AllDirectories)
                .Select(async path => new
                {
                    Key = Path.GetRelativePath(targetDir, path),
                    Bytes = await File.ReadAllBytesAsync(path, ct)
                }));

        var snapshot = new GitHubReleaseSnapshot(
            release.TagName,
            files.ToDictionary(f => f.Key, f => f.Bytes));

        var previous = Interlocked.Exchange(ref _current, snapshot);

        // Clean up older extracted releases
        Directory.EnumerateDirectories(_baseDir)
            .Where(d => !string.Equals(d, targetDir, StringComparison.OrdinalIgnoreCase))
            .ToList()
            .ForEach(d =>
            {
                try
                {
                    Directory.Delete(d, recursive: true);
                }
                catch (IOException)
                {
                    // still in use, ignore
                }
            });

        return previous is null || previous.Tag != snapshot.Tag;
    }

    /// <summary>Polls until cancelled.</summary>
    public async Task RunAsync(TimeSpan interval, CancellationToken ct)
    {
        using var timer = new PeriodicTimer(interval);

        do
        {
            try
            {
                if (await CheckForUpdateAsync(ct))
                {
                    Console.WriteLine($"Loaded release {Current!.Tag} ({Current.Files.Count} files)");
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Update check failed: {ex.Message}");
            }
        }
        while (await timer.WaitForNextTickAsync(ct));
    }

    public void Dispose() => _http.Dispose();
}