using MemoryPack;
using MTS.Data;

namespace MTS.Models;
[MemoryPackable]
public partial class MailRewardFurnace : IMailReward
{
    public FurnaceData FurnaceData { get; set; }
    public async Task ClaimReward(CharacterData characterData, AppDbContext db)
    {
        characterData.InventoryManager.AddFurnace(FurnaceData);
    }
}