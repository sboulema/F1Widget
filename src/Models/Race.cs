namespace F1Widget.Models;

public class Race
{
    public string Location { get; set; } = string.Empty;

    public Dictionary<string, DateTime> Sessions { get; set; } = [];
}