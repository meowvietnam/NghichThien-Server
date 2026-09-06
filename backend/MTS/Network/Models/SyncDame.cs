using MemoryPack;
using Network.Models;

namespace MTS.Network.Models
{
    [MemoryPackable]
    public partial class SyncDame
    {
        public int CharSendDame { get; set; }
        public int CharTakeDame { get; set; }
        public List<DameData> Datas { get; set; }
    }
}