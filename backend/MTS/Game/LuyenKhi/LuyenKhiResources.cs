using MTS.Base;
using MTS.Game.LuyenDan;
using MTS.Models;
using Network;
using Network.Extensions;

namespace MTS.Game.LuyenKhi
{
    public class LuyenKhiResources : IMemorySerializable<LuyenKhiResources>
    {
        public MaterialGradeConfig[] MaterialGradeConfigs { get; private set; }
        public EquipmentTierConfig[] EquipmentTiers { get; private set; }
        public EquipmentGradeConfig[] EquipmentGrades { get; private set; }
        public Material[] Materials { get; private set; }
        public LuyenKhiRewardItem[] RewardItems { get; private set; }
        public LianQiEffect[] LianQiEffects { get; private set; }
        public EffectTypeConfig[] EffectTypeConfigs { get; private set; }
        public StringDict Strings { get; private set; } = new();

        public static LuyenKhiResources Instance => GameConfig.LuyenKhiResources;


        public IItemData FindItem(ItemRef itemRef)
        {
            var type = itemRef.Type;
            if (type == ItemType.Material)
            {
                return Materials.First(x => x.Id == itemRef.Id);
            }

            return null;
        }

        public void Deserialize(IReader reader, ref LuyenKhiResources value)
        {
            value.MaterialGradeConfigs = reader.ReadArray<MaterialGradeConfig>();
            value.EquipmentTiers = reader.ReadArray<EquipmentTierConfig>();
            value.EquipmentGrades = reader.ReadArray<EquipmentGradeConfig>();
            value.Materials = reader.ReadArray<Material>();
            value.RewardItems = reader.ReadArray<LuyenKhiRewardItem>();
            value.LianQiEffects = reader.ReadArray<LianQiEffect>();
            value.EffectTypeConfigs = reader.MemRead<EffectTypeConfig[]>();
            value.Strings = reader.Read<StringDict>();
        }

        public int ByteSize()
        {
            return MaterialGradeConfigs.ByteSize() + EquipmentTiers.ByteSize()
                                                   + EquipmentGrades.ByteSize() + Materials.ByteSize()
                                                   + RewardItems.ByteSize()
                                                   + LianQiEffects.ByteSize()
                                                   + EffectTypeConfigs.ByteSize() + Strings.ByteSize();
        }

        public void Serialize(IWriter writer)
        {
            writer.WriteArray(MaterialGradeConfigs);
            writer.WriteArray(EquipmentTiers);
            writer.WriteArray(EquipmentGrades);
            writer.WriteArray(Materials);
            writer.WriteArray(RewardItems);
            writer.WriteArray(LianQiEffects);
            writer.MemWrite(EffectTypeConfigs);
            writer.Write(Strings);
        }

        public Material FindMaterial(ushort id)
        {
            return Materials.FirstOrDefault(x => x.Id == id);
        }
    }
}