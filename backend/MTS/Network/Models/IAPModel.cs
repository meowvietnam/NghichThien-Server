using MemoryPack;

namespace MTS.Network.Models
{
    [MemoryPackable]
    public partial class IAPModel
    {

        public string ReceiptId { get; set; }
        public string ProductId { get; set; }



    }
}
