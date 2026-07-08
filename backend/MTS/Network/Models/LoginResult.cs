using MemoryPack;
using MTS.Models;
using Network;
using Network.Extensions;

namespace MTS.Network.Models
{
    [MemoryPackable]
    public partial class LoginResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public UserWrapper UserData { get; set; }
        public string WelcomeText { get; set; }
    }
}