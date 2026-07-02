using MemoryPack;

namespace MTS.Network.Models;

[MemoryPackable]
public partial class DoneQuestResult
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public long QuestId { get; set; }
}