using MemoryPack;

namespace MTS.Network.Models
{
    [MemoryPackable]
    public partial class LogoutResult
    {
        public bool Success { get; set; }  
        public string Message { get; set; }
    }
}