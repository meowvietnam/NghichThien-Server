using MTS.Exceptions;
using MTS.Game.Algorithm;
using Network;
using Network.Extensions;

namespace MTS.Game.AllMaps;

public class MapManager : IMemorySerializable<MapManager>
{
    public Map[] Maps { get; set; }
    private Dictionary<Map,List<Map>> Graphs = new Dictionary<Map,List<Map>>();
    private Dictionary<int,Map> MapsDict = new Dictionary<int, Map>();
    private const string MapNotFound = "Map không tồn tại";
    private const string PathNotFound = "Không tìm thấy dường đi tới";
    public void Deserialize(IReader reader, ref MapManager value)
    {
        var length = reader.Read<int>();
        Maps = new Map[length];
        for (var i = 0; i < Maps.Length; i++)
        {
            var mapType = reader.Read<MapType>();
            if (mapType == MapType.Normal)
            {
                var map = new NormalMap();
                map.Deserialize(reader, ref map);
                Maps[i] = map;
            }
            else if (mapType == MapType.Stronghold)
            {
                var map = new Stronghold();
                map.Deserialize(reader, ref map);
                Maps[i] = map;
            }
        }
        foreach (var map in Maps)
        {
            MapsDict[map.Id] = map;
        }
        foreach (var map in Maps)
        {
            var nodes = map.NextMapIds.Select(x => MapsDict[x]).ToList();
            Graphs.Add(map,nodes);
        }
    }
    public List<Map> FindPath(int startId, int targetId)
    {
        if (!MapsDict.TryGetValue(startId, out var start))
        {
            throw new InvalidException(MapNotFound);
        }
        if (!MapsDict.TryGetValue(targetId, out var target))
        {
            throw new InvalidException(MapNotFound);
        }
        var path = BFSPath.FindPath(Graphs,start,target);
        if(path == null ||  path.Count == 0)
        {
            throw new InvalidException(PathNotFound);
        }
        return path;
    }
    public int ByteSize()
    {
        return Maps.ByteSize();
    }

    public void Serialize(IWriter writer)
    {
    }

    public override string ToString()
    {
        return string.Join("\n", Maps.Select(map => map.ToString()));
    }
}