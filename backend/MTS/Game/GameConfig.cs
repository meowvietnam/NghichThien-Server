using MTS.Converter;
using MTS.Game.AllMaps;
using MTS.Game.CreateCharacter;
using MTS.Models;
using Network;
using Network.Extensions;

namespace MTS.Game;

public static class GameConfig
{
    public static CharacterData CharacterTemplate { get; private set; }
    public static StepConfig StepConfig { get; private set; }
    public static SkinResources SkinResources { get; private set; }
    public static readonly float[] UnselectPercents = { 11,12,14,16,20 };
    public static MapManager MapManager { get; private set; }
    public const int OriginalMoney = 20;
    public static void Init()
    {
        CharacterTemplate = Json.Deserialize<CharacterData>(File.ReadAllText("Data/character-template.json"));
        var reader = new Reader(File.ReadAllBytes("Data/step-config.bin"));
        StepConfig = reader.Read<StepConfig>();
        reader = new Reader(File.ReadAllBytes("Data/skin-resources.bin"));
        SkinResources = reader.Read<SkinResources>();
        reader = new Reader(File.ReadAllBytes("Data/maps.bin"));
        MapManager = reader.Read<MapManager>();
    }
}