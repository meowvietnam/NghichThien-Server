using Network;
using Network.Extensions;

namespace MTS.Game.LuyenDan;

public class TempFurnaceData : IItemData, IMemorySerializable<TempFurnaceData>
{
    public int Id { get; private set; }
    public int PricePerDurability { get; private set; }
    public string Name { get; private set; }
    public Tier Tier { get; private set; }
    public string Description { get; private set; }

    public void Deserialize(IReader reader, ref TempFurnaceData value)
    {
        value.Id = reader.Read<int>();
        value.PricePerDurability = reader.Read<int>();
        value.Name = reader.Read();
        value.Tier = reader.Read<Tier>();
        value.Description = reader.Read();
    }

    public int ByteSize()
    {
        return Id.ByteSize() + PricePerDurability.ByteSize()
                             + Name.ByteSize() + Tier.ByteSize() + Description.ByteSize();
    }

    public void Serialize(IWriter writer)
    {
        writer.Write(Id);
        writer.Write(PricePerDurability);
        writer.Write(Name);
        writer.Write(Tier);
        writer.Write(Description);
    }
}