using Network;

namespace MTS.Game.AllMaps;

public class NormalMap : Map, IMemorySerializable<NormalMap>
{
    public void Deserialize(IReader reader, ref NormalMap value)
    {
        value.MapType = MapType.Normal;
        var m = value as Map;
        base.Deserialize(reader, ref m);
    }
}