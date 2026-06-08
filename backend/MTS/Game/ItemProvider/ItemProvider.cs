using MTS.Models;

namespace MTS.Game.ItemProvider
{
    public interface IItemProvider
    {
        ItemType SupportedType { get; }
        int GetPriceItemSell(ushort id);

        int GetPriceItemBuy(ushort id);
    }

    public abstract class ItemProvider<T> : IItemProvider
    {
        public abstract T GetDataItemById(ushort id);
        public abstract ItemType SupportedType { get; }
        public abstract int GetPriceItemSell(ushort id);

        public virtual int GetPriceItemBuy(ushort id)
        {
            return GetPriceItemSell(id) * 3;
        }
    }
}