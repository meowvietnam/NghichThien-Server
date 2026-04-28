using MTS.Models;
using MTS.Utils;
using Network;
using Network.Extensions;

namespace MTS.Network.Models;

public class InventoryModify : InventoryTracking, IMemorySerializable<InventoryModify>
{
    private HashSet<T> Deserialize<T>(IReader reader) where T : IMemorySerializable<T>, new()
    {
        return reader.ReadArray<T>().ToHashSet();
    }

    private Modify<T> DeserializeModify<T>(IReader reader) where T : class, IMemorySerializable<T>, new()
    {
        return new Modify<T>
        {
            Adds = Deserialize<T>(reader),
            Updates = Deserialize<T>(reader),
            Deletes = Deserialize<T>(reader)
        };
    }

    private void SerializeModify<T>(IWriter writer, Modify<T> modify) where T : class, IMemorySerializable<T>, new()
    {
        writer.WriteArray(modify.Adds);
        writer.WriteArray(modify.Updates);
        writer.WriteArray(modify.Deletes);
    }

    private int ByteSize<T>(Modify<T> value) where T : class, IMemorySerializable<T>, new()
    {
        return value.Adds.ByteSize() + value.Updates.ByteSize() + value.Deletes.ByteSize();
    }

    public void Deserialize(IReader reader, ref InventoryModify value)
    {
        value.Furnaces = DeserializeModify<FurnaceData>(reader);
        value.Items = DeserializeModify<ItemRef>(reader);
        value.NgoDaos = DeserializeModify<NgoDao>(reader);
        value.CamNgos = DeserializeModify<CamNgo>(reader);
        value.CongPhaps = DeserializeModify<CongPhap>(reader);
        value.ThanThongs = DeserializeModify<ThanThong>(reader);
        value.Equipments = DeserializeModify<Equipment>(reader);
    }

    public int ByteSize()
    {
        return ByteSize(Furnaces) + ByteSize(Items)
                                  + ByteSize(NgoDaos) + ByteSize(CamNgos) + ByteSize(CongPhaps)
                                  + ByteSize(ThanThongs) + ByteSize(Equipments);
    }
    public void Serialize(IWriter writer)
    {
        SerializeModify(writer, Furnaces);
        SerializeModify(writer, Items);
        SerializeModify(writer, NgoDaos);
        SerializeModify(writer, CamNgos);
        SerializeModify(writer, CongPhaps);
        SerializeModify(writer, ThanThongs);
        SerializeModify(writer, Equipments);
    }
}