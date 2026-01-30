namespace F1Widget.Models;

public class Race
{
    public string Location { get; set; } = string.Empty;

    public double Latitude { get; set; }

    public double Longitude { get; set; }

    public Dictionary<string, DateTime> Sessions { get; set; } = [];
}