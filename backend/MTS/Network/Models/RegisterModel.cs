using MemoryPack;

namespace MTS.Network.Models
{
    [MemoryPackable]
    public partial class RegisterModel
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }
}