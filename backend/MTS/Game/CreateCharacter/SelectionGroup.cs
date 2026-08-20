using MemoryPack;
using Network;
using Network.Extensions;

namespace MTS.Game.CreateCharacter;
[MemoryPackable]
public partial class SelectionGroup
{
    public int Id { get; private set; }
    public List<int> Items { get; set; }
    public SelectionGroup(int id) {
        Id = id;
        Items = new List<int>();
    }
    [MemoryPackConstructor]
    public SelectionGroup()
    {
    }
}