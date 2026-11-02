using System.Text;
using F1Widget.Caches;
using F1Widget.Extensions;
using F1Widget.Models;
using F1Widget.Models.F1DB;
using Microsoft.AspNetCore.Http.HttpResults;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

var destinationTimeZone = TimeZoneInfo.FindSystemTimeZoneById(app.Configuration["TZ"] ?? "Etc/UTC");

using var cache = new GitHubReleaseCache("f1db", "f1db", "f1db-json-splitted.zip");
using var cts = new CancellationTokenSource();

_ = cache.RunAsync(TimeSpan.FromMinutes(15), cts.Token);

app.MapGet("/next", async Task<Results<Ok<NextRaceResponse>, NotFound>>() =>
{
    var nextRace = await GetNextRace();

    if (nextRace == null)
    {
        return TypedResults.NotFound();
    }

    var response =
        new NextRaceResponse
        {
            Race = new()
            {
                Location = GetSessionName(nextRace.OfficialName),
                FP1 = GetSessionDateTime(nextRace.FreePractice1Date, nextRace.FreePractice1Time),
                FP2 = GetSessionDateTime(nextRace.FreePractice2Date, nextRace.FreePractice2Time),
                FP3 = GetSessionDateTime(nextRace.FreePractice3Date, nextRace.FreePractice3Time),
                Qualifying = GetSessionDateTime(nextRace.QualifyingDate, nextRace.QualifyingTime),
                Sprint = GetSessionDateTime(nextRace.SprintRaceDate, nextRace.SprintRaceTime),
                GP = GetSessionDateTime(nextRace.Date, nextRace.Time),
            },
            Circuit = new()
            {
                LayoutSvgUrl = $"https://raw.githubusercontent.com/f1db/f1db/refs/heads/main/src/assets/circuits/white/{nextRace.CircuitLayoutId}.svg",
            },
        };

    return TypedResults.Ok(response);
});

app.Run();

async Task<Race?> GetNextRace()
{
    var races = cache.Current?.GetJson<IEnumerable<Race>>("f1db-races.json");

    if (races == null)
    {
        return null;
    }

    var nextRace = races
        .OrderBy(race => race.Date)
        .FirstOrDefault(race => race.Date >= DateOnly.FromDateTime(DateTime.UtcNow));

    return nextRace;
}

DateTime? GetSessionDateTime(DateOnly? date, TimeOnly? time)
    => date == null || time == null
        ? null
        : TimeZoneInfo.ConvertTimeFromUtc(date.Value.ToDateTime(time.Value), destinationTimeZone);

string GetSessionName(string officialName)
{
    var sb = new StringBuilder(officialName);

    sb.Replace("Formula 1", string.Empty);

    sb
        .Replace("Grand Prix", string.Empty)
        .Replace("Gran Premio de", string.Empty)
        .Replace("Grande Prêmio de", string.Empty);

    sb.Replace(DateTime.UtcNow.Year.ToString(), string.Empty);

    if (sb.Length > 0)
    {
        sb[0] = char.ToUpper(sb[0]);
    }

    return sb.ToString().Trim();
}