using Network;
using Network.Extensions;

namespace MTS.Game.AllMaps;

public class Map : IMemorySerializable<Map>
{
    public MapType MapType { get; set; }
    public int Id { get; set; }
    public int[] NextMapIds { get; set; }

    public int ByteSize()
    {
        return MapType.ByteSize() + Id.ByteSize() + NextMapIds.ByteSize(); 
    }

    public void Serialize(IWriter writer)
    {
        writer.Write(MapType);
        writer.Write(Id);
        writer.WriteArray(NextMapIds);
    }
    public void Deserialize(IReader reader, ref Map value)
    {
        value.Id = reader.Read<int>();
        value.NextMapIds = reader.ReadArray<int>();
    }

    public override string ToString()
    {
        return $"SelfId: {Id}, MapType: {MapType}, NextMapIds: [{string.Join(", ", NextMapIds)}]";
    }

    public override int GetHashCode()
    {
        return Id;
    }
}