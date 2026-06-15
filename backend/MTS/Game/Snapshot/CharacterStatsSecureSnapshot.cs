using MemoryPack;
using MTS.Models;

namespace MTS.Game.Snapshot;

[MemoryPackable]
public partial class CharacterStatsSecureSnapshot
{
    public int Id { get; set; }
    public Gender Gender { get; set; }
    public string Name { get; set; }
    public EMonPhai MonPhai { get; set; }
    public ProgressionSnapshot Progression { get; set; } = new();
    public StatsSnapshot Stats { get; set; } = new();
    public AppearanceSnapshot Appearance { get; set; } = new();
    public InventorySnapshot Inventory { get; set; } = new();
    public EquipmentSnapshot Equipment { get; set; } = new();

    [MemoryPackable]
    public partial class ProgressionSnapshot
    {
        public int Level { get; set; }
        public int CurrentExp { get; set; }
        public int NgoDaoDiem { get; set; }
        public int IdCurrentNode { get; set; }
    }

    [MemoryPackable]
    public partial class StatsSnapshot
    {
        public float Kim { get; set; }
        public float Moc { get; set; }
        public float Thuy { get; set; }
        public float Hoa { get; set; }
        public float Tho { get; set; }
        public float Ma { get; set; }
        public int TuChat { get; set; }
        public int NgoTinh { get; set; }
        public int ThanThuc { get; set; }
        public int TocDoBay { get; set; }
        public int LinhThach { get; set; }
        public int Hp { get; set; }
        public int SinhMenh { get; set; }
        public int ThoNguyen { get; set; }
        public int TamCanh { get; set; }
        public LinhCan LinhCan { get; set; }
        public Ability Ability { get; set; }
        public int DanDoc { get; set; }
        public TimeSpan TuoiTac { get; set; }
    }

    [MemoryPackable]
    public partial class AppearanceSnapshot
    {
        public Dictionary<string, string> Skins { get; set; } = new();
        public Dictionary<string, Color> Colors { get; set; } = new();
    }

    [MemoryPackable]
    public partial class InventorySnapshot
    {
        public HashSet<CamNgo> CamNgos { get; set; } = new();
        public HashSet<CongPhap> CongPhapOwners { get; set; } = new();
        public HashSet<ThanThong> ThanThongOwners { get; set; } = new();
        public HashSet<Equipment> VuKhis { get; set; } = new();
        public HashSet<Equipment> PhongCus { get; set; } = new();
        public HashSet<Equipment> TrangSucs { get; set; } = new();
    }

    [MemoryPackable]
    public partial class EquipmentSnapshot
    {
        public HashSet<ThanThongEquip> ThanThongEquips { get; set; } = new();
        public HashSet<DanCombatUsed> DanCombatUseds { get; set; } = new();
        public ushort DonThuatUsed { get; set; } = ushort.MaxValue;
        public ushort PhuTu1Used { get; set; } = ushort.MaxValue;
        public ushort PhuTu2Used { get; set; } = ushort.MaxValue;
        public ushort PhuTu3Used { get; set; } = ushort.MaxValue;
        public ushort PhuTu4Used { get; set; } = ushort.MaxValue;
        public ushort ChuTu1Used { get; set; } = ushort.MaxValue;
        public ushort ChuTu2Used { get; set; } = ushort.MaxValue;
        public ushort VuKhi { get; set; } = ushort.MaxValue;
        public ushort PhongCu { get; set; } = ushort.MaxValue;
        public ushort TrangSuc { get; set; } = ushort.MaxValue;
    }
}