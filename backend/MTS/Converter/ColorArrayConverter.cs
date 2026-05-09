using MTS.Models;
using Newtonsoft.Json;
namespace MTS.Converter;

public class ColorArrayConverter : Newtonsoft.Json.JsonConverter<Color>
{
    public override void WriteJson(JsonWriter writer, Color value, JsonSerializer serializer)
    {
        writer.WriteStartArray();
        writer.WriteValue(value.R);
        writer.WriteValue(value.G);
        writer.WriteValue(value.B);
        writer.WriteValue(value.A);
        writer.WriteEndArray();
    }
    public override Color ReadJson(JsonReader reader, Type objectType, Color existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        var arr = serializer.Deserialize<List<float>>(reader);
        return new Color
        {
            R = arr[0],
            G = arr[1],
            B = arr[2],
            A = arr.Count > 3 ? arr[3] : 1f
        };
    }
}