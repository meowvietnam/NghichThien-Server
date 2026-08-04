using MemoryPack;

namespace MTS.Network.Models;
[MemoryPackable]
public partial class WatchAdsSideQuestResult : Result
{
    public int AddMoney { get; set; }
    public int AddMoneyVip { get; set; }
    public int Money { get; set; }
    public int MoneyVip { get; set; }
}