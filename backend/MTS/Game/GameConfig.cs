using MTS.Converter;
using MTS.Game.AllMaps;
using MTS.Game.CreateCharacter;
using MTS.Game.LuyenDan;
using MTS.Game.LuyenKhi;
using MTS.Models;
using Network;
using Network.Extensions;

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
    public static readonly float[] UnselectPercents = { 11,12,14,16,20 };
    public const int OriginalMoney = 20;

    private static T Read<T>(string filePath) where T : IMemorySerializable<T>, new()
    {
        var reader = new Reader(File.ReadAllBytes(filePath));
        return reader.Read<T>();
    }
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
    }
}