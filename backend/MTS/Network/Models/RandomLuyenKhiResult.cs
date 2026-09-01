using MemoryPack;
using MTS.Models;

namespace MTS.Network.Models;

[MemoryPackable]
public partial class RandomLuyenKhiResult : Result
{
    public TimeSpan TuoiTac { get; set; }
}