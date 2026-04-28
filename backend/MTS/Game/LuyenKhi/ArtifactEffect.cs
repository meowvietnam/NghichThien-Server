using Network;
using Network.Extensions;

namespace MTS.Game.LuyenKhi
{
    [Serializable]
    public class ArtifactEffect : IMemorySerializable<ArtifactEffect>
    {
        public EquipmentType EquipmentType
        {
            get
            {
                switch (ArtifactType)
                {
                    case ArtifactType.PhiKiem:
                    case ArtifactType.BaoChung:
                    case ArtifactType.ChiHoan:
                    case ArtifactType.PhiCham:
                    case ArtifactType.BaoHap:
                        return EquipmentType.Weapon;
                    case ArtifactType.TruongBao:
                    case ArtifactType.GiapTru:
                        return EquipmentType.Armor;
                    default:
                        return EquipmentType.Jewelry;
                }
            }
        }
        public ArtifactType ArtifactType { get; set; }
        private EffectTypeConfig Config
        {
            get
            {
                return LuyenKhiResources.Instance.EffectTypeConfigs.First(x => x.Type == EffectType);
            }
        }
        public EffectType EffectType { get; set; }
        public string Name => Config.Name;
        public int Value => Config.Value;
        public void Deserialize(IReader reader, ref ArtifactEffect value)
        {
           value.ArtifactType = reader.Read<ArtifactType>();
           value.EffectType = reader.Read<EffectType>();
        }
        public int ByteSize()
        {
            return ArtifactType.ByteSize() + EffectType.ByteSize();
        }
        public void Serialize(IWriter writer)
        {
            writer.Write(ArtifactType);
            writer.Write(EffectType);
        }
    }
}