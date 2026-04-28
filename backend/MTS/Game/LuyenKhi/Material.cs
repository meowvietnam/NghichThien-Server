using MTS.Base;
using MTS.Game.LuyenDan;
using Network;
using Network.Extensions;

namespace MTS.Game.LuyenKhi
{
    [Serializable]
    public class Material : IItemData, IMemorySerializable<Material>
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public Tier Tier { get; set; }
        public CultivationElement CultivationElement { get; set; }
        public YinYangType YinYangType { get; set; }
        public string Description => Desc2;
        public int TypeRef { get; set; }
        public int PropertyRef { get; set; }
        public int[] OriginRef { get; set; }
        public int Desc1Ref { get; set; }
        public int Desc2Ref { get; set; }
        public int[] ItemFlags { get; set; }
        public int Price { get; set; }
        private StringDict Dict => LuyenKhiResources.Instance.Strings;
        public string Type => Dict[TypeRef];
        public string Property => Dict[PropertyRef];
        public string[] Origin => OriginRef.Select(o => Dict[o]).ToArray();
        public string Desc1 => Dict[Desc1Ref];
        public string Desc2 => Dict[Desc2Ref];
        public void Deserialize(IReader reader, ref Material value)
        {
            value.Id = reader.Read<int>();
            value.Name = reader.Read();
            value.Tier = reader.Read<Tier>();
            value.CultivationElement = reader.Read<CultivationElement>();
            value.YinYangType = reader.Read<YinYangType>();
            value.TypeRef = reader.Read<int>();
            value.PropertyRef = reader.Read<int>();
            value.OriginRef = reader.ReadArray<int>();
            value.Desc1Ref = reader.Read<int>();
            value.Desc2Ref = reader.Read<int>();
            value.ItemFlags = reader.ReadArray<int>();
            value.Price = reader.Read<int>();
        }

        public int ByteSize()
        {
            return Id.ByteSize() + Name.ByteSize() + Tier.ByteSize() 
                   + CultivationElement.ByteSize() + YinYangType.ByteSize() + TypeRef.ByteSize()
                   + PropertyRef.ByteSize() + OriginRef.ByteSize() + Desc1Ref.ByteSize() 
                   + Desc2Ref.ByteSize() + ItemFlags.ByteSize() + Price.ByteSize();
        }

        public void Serialize(IWriter writer)
        {
            writer.Write(Id);
            writer.Write(Name);
            writer.Write(Tier);
            writer.Write(CultivationElement);
            writer.Write(YinYangType);
            writer.Write(TypeRef);
            writer.Write(PropertyRef);
            writer.WriteArray(OriginRef);
            writer.Write(Desc1Ref);
            writer.Write(Desc2Ref);
            writer.WriteArray(ItemFlags);
            writer.Write(Price);
        }
    }
}