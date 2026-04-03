using Network;
using Network.Extensions;

namespace MTS.Game.LuyenDan;

public class HerbData : IItemData, IMemorySerializable<HerbData>
{
    public int Id { get; private set; }
    public Tier Tier { get; private set; }
    public string Name { get; private set; }
    public string ChuDuoc { get; private set; }
    public string PhuDuoc { get; private set; }
    public string ThuocDan { get; private set; }
    public string Locations { get; private set; }
    public string Description { get; private set; }

    public HerbType HerbType
    {
        get
        {
            var type = HerbType.None;
            if (!string.IsNullOrWhiteSpace(ChuDuoc))
            {
                type |= HerbType.ChuDuoc;
            }

            if (!string.IsNullOrWhiteSpace(PhuDuoc))
            {
                type |= HerbType.PhuDuoc;
            }

            if (!string.IsNullOrWhiteSpace(ThuocDan))
            {
                type |= HerbType.DuocDan;
            }

            return type;
        }
    }

    public HerbData(int id, Tier tier, string name, string chuduoc, string phuduoc, string thuocdan,
        string locations)
    {
        Id = id;
        Tier = tier;
        Name = name;
        ChuDuoc = chuduoc;
        PhuDuoc = phuduoc;
        ThuocDan = thuocdan;
        Locations = locations;
    }

    public HerbData()
    {
    }

    public bool Is(HerbType type)
    {
        var myType = HerbType;
        return myType.HasFlag(type);
    }

    public int ByteSize()
    {
        return Id.ByteSize() + Tier.ByteSize() + Name.ByteSize()
               + ChuDuoc.ByteSize() + PhuDuoc.ByteSize() + ThuocDan.ByteSize()
               + Locations.ByteSize() + Description.ByteSize();
    }

    public void Serialize(IWriter writer)
    {
        writer.Write(Id);
        writer.Write(Tier);
        writer.Write(Name);
        writer.Write(ChuDuoc);
        writer.Write(PhuDuoc);
        writer.Write(ThuocDan);
        writer.Write(Locations);
        writer.Write(Description);
    }

    public void Deserialize(IReader reader, ref HerbData value)
    {
        value.Id = reader.Read<int>();
        value.Tier = reader.Read<Tier>();
        value.Name = reader.Read();
        value.ChuDuoc = reader.Read();
        value.PhuDuoc = reader.Read();
        value.ThuocDan = reader.Read();
        value.Locations = reader.Read();
        value.Description = reader.Read();
    }
}