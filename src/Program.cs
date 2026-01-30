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
    var nextCircuitLayout = nextCircuit?.Layouts.FirstOrDefault(layout
        => int.Parse(layout.Seasons.Split("-")[0]) <= DateTime.UtcNow.Year
        && int.Parse(layout.Seasons.Split("-")[1]) >= DateTime.UtcNow.Year);
    var nextCircuitLayoutSvgUrl = $"https://raw.githubusercontent.com/julesr0y/f1-circuits-svg/refs/heads/main/circuits/white/{nextCircuitLayout.LayoutId}.svg";
    var nextCircuitLayoutPngUrl = $"{app.Configuration["BASE_URL"]}/next/img/{nextCircuitLayout.LayoutId}.png";

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

DateTime? GetSessionDateTime(Race race, string sessionName)
{
    if (!race.Sessions.TryGetValue(sessionName, out var sessionDateTime))
    {
        return null;
    }

    return TimeZoneInfo.ConvertTimeFromUtc(sessionDateTime, destinationTimeZone);
}