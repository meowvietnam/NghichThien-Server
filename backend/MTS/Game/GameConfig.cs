using MemoryPack;
using MTS.Converter;
using MTS.Game.AllMaps;
using MTS.Game.CreateCharacter;
using MTS.Game.Gacha;
using MTS.Game.LuyenDan;
using MTS.Game.LuyenKhi;
using MTS.Game.Managers;
using MTS.Game.Snapshot;
using MTS.Game.Social;
using MTS.Models;
using Network;
using Network.Extensions;
using Newtonsoft.Json;
using MTS.Game.Gacha;

namespace MTS.Game;

public static class GameConfig
{
    public static CharacterData CharacterTemplate { get; private set; }
    public static StepConfig StepConfig { get; private set; }
    public static SkinResources SkinResources { get; private set; }
    public static MapManager MapManager { get; private set; }
    public static LuyenDanResources LuyenDanResources { get; private set; }
    public static LuyenKhiResources LuyenKhiResources { get; private set; }
    public static NDCPTTManager NDCPTTManager { get; private set; }
    public static NgoDaoManager NgoDaoManager { get; private set; }
    public static MainQuestManagerModel MainQuestManagerModel { get; private set; }
    public static ComboSkillResource ComboSkillResources { get; private set; }
    public static NpcData[] NpcDatas { get; private set; }

    public static readonly float[] UnselectPercents = { 11, 12, 14, 16, 20 };
    public const int OriginalMoney = 20;

    public static readonly string[] tamCanh = new string[]
    {
        "Tâm phiền ý loạn",
        "Thần tĩnh khí an",
        "Không một gợn sóng",
        "Tâm tình yên lặng",
        "Khám phá hồng trần",
        "Cao siêu ngoại vật",
        "Vật ta hợp nhất",
    };

    public static readonly int[] tamCanhRequireEnd = new int[]
    {
        0,
        49,
        199,
        599,
        999,
        1999,
    };

    public static readonly string[] danDocText = new string[]
    {
        "Thân thể khỏe mạnh",
        "Thân thể không ngại",
        "Có chút khó chịu",
        "Gân mạch ngưng trệ",
        "Bệnh nguy kịch",
        "Độc phát thân vong",
    };

    public static readonly int[] danDocRequireEnd = new int[]
    {
        20,
        50,
        70,
        100,
        120,
    };

    public static readonly InfoLevel[] infoLevels = new InfoLevel[]
    {
        new InfoLevel
            { tuviRequireNextLevel = 20000, hpMaxBuff = 15, thanThucBuff = 1, donTocBuff = 1, thoNguyenBuff = 0 },
        new InfoLevel
            { tuviRequireNextLevel = 50000, hpMaxBuff = 15, thanThucBuff = 1, donTocBuff = 1, thoNguyenBuff = 0 },
        new InfoLevel
            { tuviRequireNextLevel = 144000, hpMaxBuff = 80, thanThucBuff = 2, donTocBuff = 2, thoNguyenBuff = 100 },
        new InfoLevel
            { tuviRequireNextLevel = 400000, hpMaxBuff = 40, thanThucBuff = 2, donTocBuff = 2, thoNguyenBuff = 0 },
        new InfoLevel
            { tuviRequireNextLevel = 800000, hpMaxBuff = 40, thanThucBuff = 2, donTocBuff = 2, thoNguyenBuff = 0 },
        new InfoLevel
            { tuviRequireNextLevel = 1200000, hpMaxBuff = 300, thanThucBuff = 8, donTocBuff = 8, thoNguyenBuff = 300 },
        new InfoLevel
            { tuviRequireNextLevel = 1577000, hpMaxBuff = 150, thanThucBuff = 4, donTocBuff = 4, thoNguyenBuff = 0 },
        new InfoLevel
            { tuviRequireNextLevel = 3211000, hpMaxBuff = 150, thanThucBuff = 4, donTocBuff = 4, thoNguyenBuff = 0 },
        new InfoLevel
        {
            tuviRequireNextLevel = 6480000, hpMaxBuff = 800, thanThucBuff = 16, donTocBuff = 16, thoNguyenBuff = 500
        },
        new InfoLevel
            { tuviRequireNextLevel = 19440000, hpMaxBuff = 400, thanThucBuff = 8, donTocBuff = 8, thoNguyenBuff = 0 },
        new InfoLevel
            { tuviRequireNextLevel = 38880000, hpMaxBuff = 400, thanThucBuff = 8, donTocBuff = 8, thoNguyenBuff = 0 },
        new InfoLevel
        {
            tuviRequireNextLevel = 77760000, hpMaxBuff = 8000, thanThucBuff = 32, donTocBuff = 32, thoNguyenBuff = 1000
        },
        new InfoLevel
        {
            tuviRequireNextLevel = 94422000, hpMaxBuff = 4000, thanThucBuff = 16, donTocBuff = 16, thoNguyenBuff = 0
        },
        new InfoLevel
        {
            tuviRequireNextLevel = 144411000, hpMaxBuff = 4000, thanThucBuff = 16, donTocBuff = 16, thoNguyenBuff = 0
        },
        new InfoLevel
            { tuviRequireNextLevel = 200000000, hpMaxBuff = 0, thanThucBuff = 0, donTocBuff = 0, thoNguyenBuff = 0 }
    };

    public static Dictionary<BaseSkill.ETierSkill, int> PriceTTWithTier = new Dictionary<BaseSkill.ETierSkill, int>
    {
        { BaseSkill.ETierSkill.NhanGiaiHa, 25 },
        { BaseSkill.ETierSkill.NhanGiaiTrung, 50 },
        { BaseSkill.ETierSkill.NhanGiaiThuong, 75 },
        { BaseSkill.ETierSkill.DiaGiaiHa, 600 },
        { BaseSkill.ETierSkill.DiaGiaiTrung, 800 },
        { BaseSkill.ETierSkill.DiaGiaiThuong, 1000 },
        { BaseSkill.ETierSkill.ThienGiaiHa, 18000 },
        { BaseSkill.ETierSkill.ThienGiaiTrung, 30000 },
        { BaseSkill.ETierSkill.ThienGiaiThuong, 50000 },
    };

    public static readonly Dictionary<Tier, int> PriceVatLieuWithTier = new()
    {
        { Tier.NhatPham, 4 },
        { Tier.NhiPham, 36 },
        { Tier.TamPham, 265 },
        { Tier.TuPham, 2285 },
        { Tier.NguPham, 22912 },
        { Tier.LucPham, 256608 },
    };

    public static Dictionary<Tier, int> PriceHerbWithTier = new()
    {
        { Tier.NhatPham, 3 },
        { Tier.NhiPham, 12 },
        { Tier.TamPham, 135 },
        { Tier.TuPham, 1440 },
        { Tier.NguPham, 13500 },
        { Tier.LucPham, 81000 },
    };
    public static Dictionary<ERelationShip, int> relationShipRequireThanThong = new Dictionary<ERelationShip, int>
    {
        { ERelationShip.None, 1 }, // Use { key, value }
        { ERelationShip.Quen, 100 },
        { ERelationShip.BanBe, 500 },
        { ERelationShip.ThanThiet, 1000 },

    };
    public static Dictionary<ERelationShip, int> relationShipRequireCongPhap = new Dictionary<ERelationShip, int>
    {
        { ERelationShip.None, 1 }, // Use { key, value }
        { ERelationShip.Quen, 200 },
        { ERelationShip.BanBe, 1000 },
        { ERelationShip.ThanThiet, 2000 },

    };
    public static ERelationShip GetERelationShip(int myScore, bool isThanThong)
    {
        Dictionary<ERelationShip, int> relationRequire = isThanThong ? relationShipRequireThanThong : relationShipRequireCongPhap;
        return relationRequire
        .Where(pair => myScore >= pair.Value) // Lọc các mốc mà điểm của bạn đã đạt tới
        .OrderByDescending(pair => pair.Value) // Sắp xếp từ cao xuống thấp
        .FirstOrDefault() // Lấy mốc cao nhất
        .Key;
    }
    public static Dictionary<BaseSkill.ETierSkill, int> tierSkillWithLevel = new Dictionary<BaseSkill.ETierSkill, int>
    {
        { BaseSkill.ETierSkill.NhanGiaiHa, 1 },
        { BaseSkill.ETierSkill.NhanGiaiTrung, 3 },
        { BaseSkill.ETierSkill.NhanGiaiThuong, 6 },
        { BaseSkill.ETierSkill.DiaGiaiHa, 8 },
        { BaseSkill.ETierSkill.DiaGiaiTrung, 10 },
        { BaseSkill.ETierSkill.DiaGiaiThuong, 12 },
        { BaseSkill.ETierSkill.ThienGiaiHa, 13 },
        { BaseSkill.ETierSkill.ThienGiaiTrung, 14 },
        { BaseSkill.ETierSkill.ThienGiaiThuong, 15 },
    };

    public static Dictionary<Tier, int> tierItemWithLevel = new Dictionary<Tier, int>
    {
        { Tier.NhatPham, 1 },
        { Tier.NhiPham, 3 },
        { Tier.TamPham, 5 },
        { Tier.TuPham, 8 },
        { Tier.NguPham, 11 },
        { Tier.LucPham, 15 },
    };

    public static Dictionary<EquipmentTier, int> tierEquipmentWithLevel = new Dictionary<EquipmentTier, int>
    {
        { EquipmentTier.PhuKhi, 3 },
        { EquipmentTier.PhapKhi, 6 },
        { EquipmentTier.PhapBao, 9 },
        { EquipmentTier.ThuanDuong, 12 },
        { EquipmentTier.ThongThien, 15 },
    };
    public static Dictionary<Tier, int> PriceTienNgocVatLieuAndHerbTask = new Dictionary<Tier, int>
    {
        { Tier.NhatPham, 20 },
        { Tier.NhiPham, 30 },
        { Tier.TamPham, 40 },
        { Tier.TuPham, 50 },
        { Tier.NguPham, 60 },
        { Tier.LucPham, 70 },
    };
    public static Dictionary<Tier, int> PriceFurnaceWithTierPerDurability => PriceHerbWithTier;

    private static T Read<T>(string filePath) where T : IMemorySerializable<T>, new()
    {
        var reader = new Reader(File.ReadAllBytes(filePath));
        return reader.Read<T>();
    }

    public static T Read2<T>(string filePath) where T : IMemoryPackable<T>
    {
        var bytes = File.ReadAllBytes(filePath);
        return MemoryPackSerializer.Deserialize<T>(bytes);
    }
    public static T[] ReadArray2<T>(string filePath) where T : IMemoryPackable<T>
    {
        var bytes = File.ReadAllBytes(filePath);
        return MemoryPackSerializer.Deserialize<T[]>(bytes);
    }
    public static Dictionary<ETypeGacha, int> PageGachaWithRequireValue = new Dictionary<ETypeGacha, int>
    {
        {ETypeGacha.ThanThong,400},
        {ETypeGacha.CongPhap,500},
        {ETypeGacha.DanDuoc,500},
        {ETypeGacha.DuocLieu,300},
        {ETypeGacha.VatLieu,200},
    };
    public static void Init()
    {
        CharacterTemplate = Json.Deserialize<CharacterData>(File.ReadAllText("Data/character-template.json"));
        StepConfig = Read<StepConfig>("Data/step-config.bin");
        SkinResources = Read<SkinResources>("Data/skin-resources.bin");
        MapManager = Read<MapManager>("Data/maps.bin");
        LuyenDanResources = Read<LuyenDanResources>("Data/LuyenDanResources.bin");
        LuyenKhiResources = Read<LuyenKhiResources>("Data/LuyenKhiResources.bin");
        NDCPTTManager = Read<NDCPTTManager>("Data/NDCPTTManager.bin");
        NgoDaoManager = Read<NgoDaoManager>("Data/NgoDaoManager.bin");
        MainQuestManagerModel = Read<MainQuestManagerModel>("Data/MainQuestManagerModel.bin");
        ComboSkillResources = Read2<ComboSkillResource>("Data/combo-skill-resources.bin");
        NpcDatas = ReadArray2<NpcData>("Data/npc-datas.bin");
    }
}