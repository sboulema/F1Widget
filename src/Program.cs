using F1Widget.Caches;
using F1Widget.Extensions;
using F1Widget.Models;
using Geolocation;
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
                Location = nextRace.Location,
                FP1 = GetSessionDateTime(nextRace, "fp1"),
                FP2 = GetSessionDateTime(nextRace, "fp2"),
                FP3 = GetSessionDateTime(nextRace, "fp3"),
                Qualifying = GetSessionDateTime(nextRace, "qualifying"),
                Sprint = GetSessionDateTime(nextRace, "sprint"),
                GP = GetSessionDateTime(nextRace, "gp"),
                Canceled = nextRace.Canceled,
            },
        };

    var nextCircuit = GetNextCircuit(nextRace);

    if (nextCircuit != null)
    {
        var nextCircuitLayout = GetNextCircuitLayout(nextCircuit);

        response.Circuit = new()
        {
            LayoutSvgUrl = $"https://raw.githubusercontent.com/f1db/f1db/refs/heads/main/src/assets/circuits/white/{nextCircuitLayout?.Id}.svg",
        };
    }

    return TypedResults.Ok(response);
});

app.Run();

async Task<Race?> GetNextRace()
{
    var client = new HttpClient();

    var calendarURL = $"https://raw.githubusercontent.com/sportstimes/f1/refs/heads/main/_db/f1/{DateTime.UtcNow.Year}.json";
    var calendar = await client.GetFromJsonAsync<Calendar>(calendarURL);

    if (calendar == null)
    {
        return null;
    }

    var nextRace = calendar
        .Races
        .OrderBy(race => race.Sessions["fp1"])
        .FirstOrDefault(race => race.Sessions.Any(session => session.Value >= DateTime.UtcNow));

    return nextRace;
}

Circuit? GetNextCircuit(Race nextRace)
{
    var circuits = cache.Current?.GetJson<IEnumerable<Circuit>>("f1db-circuits.json");

    if (circuits == null)
    {
        return null;
    }

    var nextCircuit = circuits.MinBy(circuit => GeoCalculator.GetDistance(
        nextRace.Latitude, nextRace.Longitude,
        circuit.Latitude, circuit.Longitude));

    return nextCircuit;
}

Layout? GetNextCircuitLayout(Circuit nextCircuit)
{
    var layouts = cache.Current?.GetJson<IEnumerable<Layout>>("f1db-layouts.json");

    if (layouts == null)
    {
        return null;
    }

    var nextCircuitLayout = layouts
        .Where(layout => layout.CircuitId == nextCircuit.Id)
        .FirstOrDefault(layout => layout.Effective);

    return nextCircuitLayout;
}

DateTime? GetSessionDateTime(Race race, string sessionName)
{
    if (!race.Sessions.TryGetValue(sessionName, out var sessionDateTime))
    {
        return null;
    }

    return TimeZoneInfo.ConvertTimeFromUtc(sessionDateTime, destinationTimeZone);
}