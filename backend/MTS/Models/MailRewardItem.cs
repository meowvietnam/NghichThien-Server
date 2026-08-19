using MemoryPack;
using MTS.Data;

namespace MTS.Models;
[MemoryPackable]
public partial class MailRewardItem : IMailReward
{
    public ItemRef Item { get; set; }
    public async Task ClaimReward(CharacterData characterData, AppDbContext dbContext)
    {
        var inventory = characterData.InventoryManager;
        inventory.Add(Item);
    }
}