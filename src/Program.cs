using System.Drawing.Imaging;
using F1Widget.Models;
using Geolocation;
using Microsoft.AspNetCore.Http.HttpResults;
using Svg;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

var destinationTimeZone = TimeZoneInfo.FindSystemTimeZoneById(app.Configuration["TZ"] ?? "Etc/UTC");

app.MapGet("/next", async Task<Results<Ok<NextRaceResponse>, NotFound>>() =>
{
    var nextRace = await GetNextRace();

    if (nextRace == null)
    {
        return TypedResults.NotFound();
    }

    var nextCircuit = await GetNextCircuit(nextRace);
    var nextCircuitLayout = nextCircuit?.Layouts.FirstOrDefault(IsCurrentCircuitLayout);
    var nextCircuitLayoutSvgUrl = $"https://raw.githubusercontent.com/julesr0y/f1-circuits-svg/refs/heads/main/circuits/detailed/white/{nextCircuitLayout?.LayoutId}.svg";
    var nextCircuitLayoutPngUrl = $"{app.Configuration["BASE_URL"]}/next/img/{nextCircuitLayout?.LayoutId}.png";

    return TypedResults.Ok(
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
            Circuit = new()
            {
                LayoutSvgUrl = nextCircuitLayoutSvgUrl,
                LayoutPngUrl = nextCircuitLayoutPngUrl,
            },
        }
    );
});

app.MapGet("/next/img/{layoutId}.png", async (string layoutId) =>
{
    var svgUrl = $"https://raw.githubusercontent.com/julesr0y/f1-circuits-svg/refs/heads/main/circuits/white/{layoutId}.svg";

    var client = new HttpClient();
    using var svgStream = await client.GetStreamAsync(svgUrl);

    var svgDocument = SvgDocument.Open<SvgDocument>(svgStream);
    var bitmap = svgDocument.Draw();

    using var outputStream = new MemoryStream();
    bitmap.Save(outputStream, ImageFormat.Png);

    return Results.File(outputStream.ToArray(), "image/png");
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

async Task<Circuit?> GetNextCircuit(Race nextRace)
{
    var client = new HttpClient();

    var circuitsURL = "https://raw.githubusercontent.com/julesr0y/f1-circuits-svg/refs/heads/main/circuits.json";
    var circuits = await client.GetFromJsonAsync<IEnumerable<Circuit>>(circuitsURL);

    if (circuits == null)
    {
        return null;
    }

    var nextCircuit = circuits.MinBy(circuit => GeoCalculator.GetDistance(
        nextRace.Latitude, nextRace.Longitude,
        circuit.Latitude, circuit.Longitude));

    return nextCircuit;
}

/// <summary>
/// Checks if the given circuit layout is used in the current season.
/// </summary>
/// <remarks>1995,1957,1959,1961-1962</remarks>
bool IsCurrentCircuitLayout(Layout layout)
{
    var currentYear = DateTime.UtcNow.Year;

    return layout
        .Seasons
        .Split(',')
        .Any(season =>
        {
            // Layout is used in a single season, e.g. "2024"
            var isSuccess = int.TryParse(season, out var layoutYear);

            if (isSuccess)
            {
                return layoutYear == currentYear;
            }

            // layout is used in multiple consecutive seasons, e.g. "2020-2024"
            if (season.Contains('-'))
            {
                var startSeason = int.Parse(season.Split('-')[0]);
                var endSeason = int.Parse(season.Split('-')[1]);
                return startSeason <= currentYear && endSeason >= currentYear;
            }

            return false;
        });
}

DateTime? GetSessionDateTime(Race race, string sessionName)
{
    if (!race.Sessions.TryGetValue(sessionName, out var sessionDateTime))
    {
        return null;
    }

    return TimeZoneInfo.ConvertTimeFromUtc(sessionDateTime, destinationTimeZone);
}