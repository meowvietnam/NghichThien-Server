using MemoryPack;
using MTS.Data;

namespace MTS.Models;
[MemoryPackable]
public partial class MailSetInfinityEnergy : IMailReward
{
    public bool IsInfinityEnergy { get; set; }
    public async Task ClaimReward(CharacterData characterData, AppDbContext db)
    {
        characterData.SetIsInfiniteEnergy(IsInfinityEnergy);
    }
}