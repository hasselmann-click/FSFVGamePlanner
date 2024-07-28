using QuestPDF.Infrastructure;
using System.Text.Json;
using System.Text.Json.Serialization;

public class ColorConverter : JsonConverter<Dictionary<string, Color>>
{
    public override Dictionary<string, Color> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var colorMap = new Dictionary<string, Color>();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                continue;
            }

            var propertyName = reader.GetString();
            if (propertyName == null)
            {
                continue;
            }

            reader.Read();
            if (reader.TokenType != JsonTokenType.String)
            {
                continue;
            }

            var hexValue = reader.GetString();
            if (hexValue == null)
            {
                continue;
            }

            Color color = Color.FromHex(hexValue);
            colorMap.Add(propertyName, color);
        }

        return colorMap;
    }

    public override void Write(Utf8JsonWriter writer, Dictionary<string, Color> value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        foreach (var kvp in value)
        {
            writer.WriteString(kvp.Key, "#" + kvp.Value.Hex.ToString("X6"));
        }

        writer.WriteEndObject();
    }
}
