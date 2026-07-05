namespace MTS.Game;

public class LevelResource : IMemorySerializable<LevelResource>
{
    public InfoLevel[] infoLevels;
    public static LevelResource Instance => GameConfig.LevelResource;
    public void Deserialize(IReader reader, ref LevelResource value)
    {
        value.infoLevels = reader.ReadArray<InfoLevel>();
    }

    public int ByteSize()
    {
        return infoLevels.ByteSize();
    }

    public void Serialize(IWriter writer)
    {
        writer.WriteArray(infoLevels);
    }
}