using MTS.Models;
using Network;
using Network.Extensions;

namespace MTS.Game.LuyenDan;

public class LuyenDanResources : IMemorySerializable<LuyenDanResources>
{
    public List<TempFurnaceData> TempFurnaceDatas { get; private set; }
    public List<HerbData> HerbDatas { get; private set; }
    public List<LinhDan> LinhDans { get; private set; }
    public List<RecipeData> Recipes { get; private set; }
    public const string EmptyDetail = "Không";
    public static LuyenDanResources Instance => GameConfig.LuyenDanResources;
    public IItemData FindItem(ItemRef itemRef)
    {
        if (itemRef.Type == ItemType.Furnace)
        {
            return TempFurnaceDatas.FirstOrDefault(x => x.Id == itemRef.Id);
        }

        if (itemRef.Type == ItemType.Herb)
        {
            return HerbDatas.FirstOrDefault(x => x.Id == itemRef.Id);
        }

        if (itemRef.Type == ItemType.LinhDan)
        {
            return LinhDans.FirstOrDefault(x => x.Id == itemRef.Id);
        }

        if (itemRef.Type == ItemType.Recipe)
        {
            return Recipes.FirstOrDefault(x => x.Id == itemRef.Id);
        }

        return null;
    }
    public IItemData FindItem(FurnaceData furnace)
    {
        return TempFurnaceDatas.FirstOrDefault(x => x.Id == furnace.Id);
    }
    public HerbData FindHerb(int id)
    {
        var herb = HerbDatas.FirstOrDefault(x => x.Id == id);
        return herb;
    }

    public string GetHerbDetail(int id, HerbType herbType)
    {
        var herb = HerbDatas.FirstOrDefault(x => x.Id == id);
        if (herb == null)
        {
            return EmptyDetail;
        }

        switch (herbType)
        {
            case HerbType.ChuDuoc:
                return herb.ChuDuoc;
            case HerbType.PhuDuoc:
                return herb.PhuDuoc;
            case HerbType.DuocDan:
                return herb.ThuocDan;
        }

        return EmptyDetail;
    }

    public RecipeData FindRecipe(int i)
    {
        return Recipes.FirstOrDefault(x => x.Id == i);
    }

    public void Deserialize(IReader reader, ref LuyenDanResources value)
    {
        value.TempFurnaceDatas = reader.ReadArray<TempFurnaceData>().ToList();
        value.HerbDatas = reader.ReadArray<HerbData>().ToList();
        value.LinhDans = reader.ReadArray<LinhDan>().ToList();
        value.Recipes = reader.ReadArray<RecipeData>().ToList();
    }

    public int ByteSize()
    {
        return TempFurnaceDatas.ByteSize() + HerbDatas.ByteSize() + LinhDans.ByteSize() + Recipes.ByteSize();
    }

    public void Serialize(IWriter writer)
    {
        writer.WriteArray(TempFurnaceDatas);
        writer.WriteArray(HerbDatas);
        writer.WriteArray(LinhDans);
        writer.WriteArray(Recipes);
    }

    public LinhDan FindLinhDan(ushort id)
    {
        return LinhDans.FirstOrDefault(x => x.Id == id);
    }
}