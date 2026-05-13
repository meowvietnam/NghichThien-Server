using Network;
using Network.Extensions;

namespace MTS.Game.LuyenDan;

public class LinhDan : IItemData, IMemorySerializable<LinhDan>
{
    public int Id { get; private set; }
    public string Name { get; private set; }
    public Tier Tier { get; private set; }
    public string Description { get; private set; }
    public int DanDoc { get; private set; }
    public int NhinThuoc { get; private set; }
    public int PriceV104 { get; private set; }
    public int PriceV130 { get; private set; }
    public int Price { get; private set; }
    public int Profit { get; private set; }
    public float Income { get; private set; }
    public int ProfitPerDay { get; private set; }
    public int ProfitDanThanh { get; private set; }
    public int ProfitDanThanhPerDay { get; private set; }
    public float IncomeDanThanh { get; private set; }
    public ETypeDan ETypeDan { get; private set; }
    public int BuffValue { get; private set; }
    public EDanBuffType EBuffType { get; private set; }

    public int ByteSize()
    {
        return Id.ByteSize() + Name.ByteSize() + Tier.ByteSize() + Description.ByteSize()
               + DanDoc.ByteSize() + NhinThuoc.ByteSize() + PriceV104.ByteSize() + PriceV130.ByteSize()
               + Price.ByteSize() + Profit.ByteSize() + Income.ByteSize()
               + ProfitPerDay.ByteSize() + ProfitDanThanh.ByteSize() + ProfitDanThanhPerDay.ByteSize()
               + IncomeDanThanh.ByteSize() + ETypeDan.ByteSize()
               + BuffValue.ByteSize() + EBuffType.ByteSize();
    }

    public void Serialize(IWriter writer)
    {
        writer.Write(Id);
        writer.Write(Name);
        writer.Write(Tier);
        writer.Write(Description);
        writer.Write(DanDoc);
        writer.Write(NhinThuoc);
        writer.Write(PriceV104);
        writer.Write(PriceV130);
        writer.Write(Price);
        writer.Write(Profit);
        writer.Write(Income);
        writer.Write(ProfitPerDay);
        writer.Write(ProfitDanThanh);
        writer.Write(ProfitDanThanhPerDay);
        writer.Write(IncomeDanThanh);
        writer.Write(ETypeDan);
        writer.Write(BuffValue);
        writer.Write(EBuffType);
    }

    public void Deserialize(IReader reader, ref LinhDan value)
    {
        value.Id = reader.Read<int>();
        value.Name = reader.Read();
        value.Tier = reader.Read<Tier>();
        value.Description = reader.Read();
        value.DanDoc = reader.Read<int>();
        value.NhinThuoc = reader.Read<int>();
        value.PriceV104 = reader.Read<int>();
        value.PriceV130 = reader.Read<int>();
        value.Price = reader.Read<int>();
        value.Profit = reader.Read<int>();
        value.Income = reader.Read<float>();
        value.ProfitPerDay = reader.Read<int>();
        value.ProfitDanThanh = reader.Read<int>();
        value.ProfitDanThanhPerDay = reader.Read<int>();
        value.IncomeDanThanh = reader.Read<float>();
        value.ETypeDan = reader.Read<ETypeDan>();
        value.BuffValue = reader.Read<int>();
        value.EBuffType = reader.Read<EDanBuffType>();
    }
}