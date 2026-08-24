using MemoryPack;
using MTS.Models;

namespace MTS.Network.Models;

[MemoryPackable]
public partial class AddWateringHelp
{
    public int SlotIndex { get; set; }
    public WateringHelp WateringHelp { get; set; }
}