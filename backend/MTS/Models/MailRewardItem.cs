using MemoryPack;

namespace MTS.Models;
[MemoryPackable]
public partial class MailRewardItem : IMailReward
{
    public ItemRef Item { get; set; }
    public void ClaimReward(CharacterData characterData)
    {
        var inventory = characterData.InventoryManager;
        inventory.Add(Item);
    }
}