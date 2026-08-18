using MemoryPack;
using MTS.Data;

namespace MTS.Models;
[MemoryPackable]
public partial class MailSetAds : IMailReward
{
    public bool NoAds { get; set; }
    public async Task ClaimReward(CharacterData characterData, AppDbContext db)
    {
        characterData.SetNoAds(NoAds);
    }
}