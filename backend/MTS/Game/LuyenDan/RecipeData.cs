using Network;
using Network.Extensions;

namespace MTS.Game.LuyenDan;

    public class RecipeData : IItemData, IMemorySerializable<RecipeData>
    {
        [Serializable]
        public class Herbs : IMemorySerializable<Herbs>
        {
            public int Id { get; private set; }
            public int Count { get; private set; }
            public int BaDuoc { get; private set; }

            public Herbs(int id, int count, int baDuoc)
            {
                Id = id;
                Count = count;
                BaDuoc = baDuoc;
            }

            public Herbs()
            {
            }

            public bool IsDefault()
            {
                return Id == 0;
            }

            public void Deserialize(IReader reader, ref Herbs value)
            {
                value.Id = reader.Read<int>();
                value.Count = reader.Read<int>();
                value.BaDuoc = reader.Read<int>();
            }

            public int ByteSize()
            {
                return Id.ByteSize() + Count.ByteSize() + BaDuoc.ByteSize();
            }

            public void Serialize(IWriter writer)
            {
                writer.Write(Id);
                writer.Write(Count);
                writer.Write(BaDuoc);
            }
        }

        [Serializable]
        public class Recipe : IMemorySerializable<Recipe>
        {
            public int Id { get; private set; }
            public Herbs ChuDuoc { get; private set; }
            public Herbs PhuDuoc1 { get; private set; }
            public Herbs PhuDuoc2 { get; private set; }
            public Herbs DuocDan { get; private set; }

            public void Deserialize(IReader reader, ref Recipe value)
            {
                value.Id = reader.Read<int>();
                value.ChuDuoc = reader.Read<Herbs>();
                value.PhuDuoc1 = reader.Read<Herbs>();
                value.PhuDuoc2 = reader.Read<Herbs>();
                value.DuocDan = reader.Read<Herbs>();
            }

            public int ByteSize()
            {
                return Id.ByteSize() + ChuDuoc.ByteSize() 
                                     + PhuDuoc1.ByteSize() + PhuDuoc2.ByteSize()
                                     + DuocDan.ByteSize();
            }

            public void Serialize(IWriter writer)
            {
                writer.Write(Id);
                writer.Write(ChuDuoc);
                writer.Write(PhuDuoc1);
                writer.Write(PhuDuoc2);
                writer.Write(DuocDan);
            }
        }

        public int Id { get; private set; }
        public int LinhDanId { get; set; }
        public List<Recipe> Recipes { get; private set; }

        public LinhDan LinhDan
        {
            get { return GameConfig.LuyenDanResources.LinhDans.FirstOrDefault(x => x.Id == LinhDanId); }
        }

        public string Name => $"Đan phương {LinhDan.Name}" ;

        public Tier Tier => LinhDan.Tier;

        public string Description => $"Công thức chế tạo {LinhDan.Name}";
        public void Deserialize(IReader reader, ref RecipeData value)
        {
            value.Id = reader.Read<int>();
            value.LinhDanId = reader.Read<int>();
            value.Recipes = reader.ReadArray<Recipe>().ToList();
        }

        public int ByteSize()
        {
            return Id.ByteSize() + LinhDanId.ByteSize() + Recipes.ByteSize();
        }

        public void Serialize(IWriter writer)
        {
            writer.Write(Id);
            writer.Write(LinhDanId);
            writer.WriteArray(Recipes);
        }
    }