using MemoryPack;
using MTS.IAP;

namespace MTS.Network.Models
{
    [MemoryPackable]
    public partial class IapModel
    {
        public Store Store { get; set; }
        public string ProductId { get; set; }
        public string PurchaseToken { get; set; }

        public override string ToString()
        {
            return $"Store: {Store}, ProductId: {ProductId}, PurchaseToken: {PurchaseToken}";
        }
    }
}
