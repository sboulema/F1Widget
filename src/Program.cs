using F1Widget.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .Configure<JsonOptions>(options =>
    {
        options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

var app = builder.Build();

var destinationTimeZone = TimeZoneInfo.FindSystemTimeZoneById(app.Configuration["TZ"] ?? "Etc/UTC");

app.MapGet("/next", async Task<Results<Ok<NextRaceResponse>, NotFound>>() =>
{
    var calendarURL = $"https://raw.githubusercontent.com/sportstimes/f1/refs/heads/main/_db/f1/{DateTime.UtcNow.Year}.json";

    var client = new HttpClient();
    var calendar = await client.GetFromJsonAsync<Calendar>(calendarURL);

    if (calendar == null)
    {
        return TypedResults.NotFound();
    }

    var nextRace = calendar
        .Races
        .OrderBy(race => race.Sessions["fp1"])
        .FirstOrDefault(race => race.Sessions.Any(session => session.Value >= DateTime.UtcNow));

    if (nextRace == null)
    {
        return TypedResults.NotFound();
    }

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
            }
        }
    );
});

app.Run();

DateTime? GetSessionDateTime(Race race, string sessionName)
{
    if (!race.Sessions.TryGetValue(sessionName, out var sessionDateTime))
    {
        return null;
    }

    return TimeZoneInfo.ConvertTimeFromUtc(sessionDateTime, destinationTimeZone);
}