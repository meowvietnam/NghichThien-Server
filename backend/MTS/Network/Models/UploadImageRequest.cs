using MemoryPack;

namespace MTS.Network.Models;
[MemoryPackable]
public partial class UploadImageRequest
{
    public byte[] Data { get; set; }
}