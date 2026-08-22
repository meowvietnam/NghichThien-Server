using MemoryPack;
using Network;
using Network.Extensions;

namespace MTS.Game.CreateCharacter;
[MemoryPackable]
public partial class SelectionModel 
{
    public int Id { get; private set; }
    public List<SelectionGroup> Groups { get; set; }

    public SelectionModel(int id)
    {
        Id = id;
        Groups = new List<SelectionGroup>();
    }
    [MemoryPackConstructor]
    public SelectionModel()
    {
    }

}