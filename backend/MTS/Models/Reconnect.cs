using MemoryPack;

namespace MTS.Models;
[MemoryPackable]
public partial class Reconnect
{
    public int Id { get; set; }
    public string Key { get; set; }
}