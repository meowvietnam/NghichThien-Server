using MemoryPack;

namespace MTS.Models;
[MemoryPackable]
public partial class MailRewardFurnace : IMailReward
{
    public FurnaceData FurnaceData { get; set; }
    public void ClaimReward(CharacterData characterData)
    {
        characterData.InventoryManager.AddFurnace(FurnaceData);
    }
}