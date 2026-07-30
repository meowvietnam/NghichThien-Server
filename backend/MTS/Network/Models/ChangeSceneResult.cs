using MemoryPack;

namespace MTS.Network.Models;

[MemoryPackable]
public partial class ChangeSceneResult
{
    public bool Success { get; set; }
    public string SceneName { get; set; }
    public string Message { get; set; }
}