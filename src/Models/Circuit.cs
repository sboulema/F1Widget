namespace F1Widget.Models;

public class Circuit
{
    public double Latitude { get; set; }

    public double Longitude { get; set; }

    public IEnumerable<Layout> Layouts { get; set; } = [];
}