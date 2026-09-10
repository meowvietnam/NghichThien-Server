using MemoryPack;

namespace MTS.Network.Models;
[MemoryPackable]
public partial class UploadImageResult : Result
{
    public string Url { get; set; }
}