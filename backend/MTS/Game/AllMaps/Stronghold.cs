using Network;

namespace MTS.Game.AllMaps;

public class Stronghold : Map, IMemorySerializable<Stronghold>
{
    public void Deserialize(IReader reader, ref Stronghold value)
    {
        value.MapType = MapType.Stronghold;
        var m = value as Map;
        base.Deserialize(reader, ref m);
    }
}