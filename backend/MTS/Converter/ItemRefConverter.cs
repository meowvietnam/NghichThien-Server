using MTS.Models;
using Newtonsoft.Json;

namespace MTS.Converter;

public class ItemRefConverter : JsonConverter<ItemRef>
{
    public override void WriteJson(JsonWriter writer, ItemRef? value, JsonSerializer serializer)
    {
        
        writer.WriteStartArray();
        writer.WriteValue(value.Id);
        writer.WriteValue(value.Type);
        writer.WriteValue(value.Count);
        writer.WriteValue(value.Durability);
        writer.WriteEndArray();
    }
    public override ItemRef? ReadJson(JsonReader reader, Type objectType, ItemRef? existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
            return null;

        if (reader.TokenType != JsonToken.StartArray)
            throw new JsonSerializationException("Expected StartArray");

        reader.Read();
        var id = Convert.ToInt32(reader.Value);

        reader.Read();
        var type = (ItemType)Convert.ToInt32(reader.Value);

        reader.Read();
        var count = Convert.ToInt32(reader.Value);

        reader.Read();
        var durability = Convert.ToInt32(reader.Value);

        reader.Read();
        if (reader.TokenType != JsonToken.EndArray)
            throw new JsonSerializationException("Expected EndArray");

        return new ItemRef
        {
            Id = id,
            Type = type,
            Count = count,
            Durability = durability
        };
    }
}