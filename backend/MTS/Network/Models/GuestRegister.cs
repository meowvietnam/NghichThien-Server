using MemoryPack;

namespace MTS.Network.Models
{
    [MemoryPackable]
    public partial class GuestRegister
    {
        public string GuestUsername { get; set; }
        public RegisterModel Model { get; set; }
    }
}