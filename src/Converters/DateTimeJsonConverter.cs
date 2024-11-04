using System.Text.Json.Serialization;
using System.Text.Json;
using System.Globalization;

namespace F1Widget.Converters;

public class DateTimeJsonConverter : JsonConverter<DateTime?>
{
    private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;
    private const string Format = "dd MMM HH:mm";

    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (!DateTime.TryParseExact(reader.GetString(), Format, Culture, DateTimeStyles.None, out var dateTime))
        {
            throw new JsonException();
        }

        return dateTime;
    }

    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value == null ? string.Empty : value.Value.ToString(Format));
    }
    
    public override bool HandleNull => true;
}