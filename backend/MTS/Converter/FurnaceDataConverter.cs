using MTS.Models;
using Newtonsoft.Json;

namespace MTS.Converter;

public class FurnaceDataConverter : JsonConverter<FurnaceData>
{
    public override void WriteJson(JsonWriter writer, FurnaceData? value, JsonSerializer serializer)
    {
        if (value == null)
        {
            writer.WriteNull();
            return;
        }
        writer.WriteStartArray();
        writer.WriteValue(value.SelfId);
        writer.WriteValue(value.Id);
        writer.WriteValue(value.Durability);
        writer.WriteEndArray();
    }

    public override FurnaceData? ReadJson(
        JsonReader reader,
        Type objectType,
        FurnaceData? existingValue,
        bool hasExistingValue,
        JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
            return null;
        if (reader.TokenType != JsonToken.StartArray)
            throw new JsonSerializationException("Expected StartArray");
        reader.Read();
        if (reader.TokenType != JsonToken.Integer)
            throw new JsonSerializationException("Expected SelfId");
        var selfId = Convert.ToUInt16(reader.Value);
        reader.Read();
        if (reader.TokenType != JsonToken.Integer)
            throw new JsonSerializationException("Expected Id");
        var id = Convert.ToUInt16(reader.Value);
        reader.Read();
        if (reader.TokenType != JsonToken.Integer)
            throw new JsonSerializationException("Expected Durability");
        var durability = Convert.ToSByte(reader.Value);
        reader.Read();
        if (reader.TokenType != JsonToken.EndArray)
            throw new JsonSerializationException("Expected EndArray");
        return new FurnaceData
        {
            SelfId = selfId,
            Id = id,
            Durability = durability,
        };
    }
}