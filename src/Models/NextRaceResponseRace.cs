using F1Widget.Converters;
using System.Text.Json.Serialization;

namespace F1Widget.Models;

public class NextRaceResponseRace
{
    public string Location { get; set; } = string.Empty;

    [JsonConverter(typeof(DateTimeJsonConverter))]
    [JsonPropertyName("fp1")]
    public DateTime? FP1 { get; set; }

    [JsonConverter(typeof(DateTimeJsonConverter))]
    [JsonPropertyName("fp2")]
    public DateTime? FP2 { get; set; }

    [JsonConverter(typeof(DateTimeJsonConverter))]
    [JsonPropertyName("fp3")]
    public DateTime? FP3 { get; set; }

    [JsonConverter(typeof(DateTimeJsonConverter))]
    public DateTime? Qualifying { get; set; }

    [JsonConverter(typeof(DateTimeJsonConverter))]
    public DateTime? Sprint { get; set; }

    [JsonConverter(typeof(DateTimeJsonConverter))]
    public DateTime? GP { get; set; }
}