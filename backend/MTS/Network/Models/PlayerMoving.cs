using MemoryPack;
using MTS.Models;

namespace MTS.Network.Models;
[MemoryPackable]
public partial class PlayerMoving
{
    public int CharId { get; set; }
    public string Name { get; set; }
    public Gender Gender { get; set; }
    public Appearance Appearance { get; set; }
    public bool IsSwordRiding { get; set; }
    public int[] Path { get; set; }
}