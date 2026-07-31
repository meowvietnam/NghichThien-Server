using MemoryPack;

namespace MTS.Network.Models
{
    [MemoryPackable]
    public partial class RegisterResult : Result
    {
        public UserWrapper UserData { get; set; }
        public string AccountAlias { get; set; }
    }
}