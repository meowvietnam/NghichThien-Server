using MemoryPack;
using MTS.Models;

namespace MTS.Network.Models;
[MemoryPackable]
public partial class GetEventResult : Result
{
    public Event Event { get; set; }
}