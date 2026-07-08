using MemoryPack;
using MTS.Models;

namespace MTS.Network.Models
{
    [MemoryPackable]
    public partial class UserWrapper
    {
        [MemoryPackIgnore]
        public UserData User { get; private set; }
        public byte[] UserBytes { get; private set; }
        public UserWrapper(UserData user)
        {
            User = user;
            var writer = new Writer(user.ByteSize());
            writer.Write(User);
            UserBytes = writer.Buffer;
        }
        [MemoryPackConstructor]
        public UserWrapper(byte[] userBytes)
        {
            UserBytes = userBytes;
            var reader = new Reader(userBytes);
            User = reader.Read<UserData>();
        }
    }
}