using MemoryPack;
using MTS.Data;
using MTS.Exceptions;
using MTS.Network.Models;

namespace MTS.Models;
[MemoryPackable]
public partial class MilestoneEvent : IGameEvent
{
    public List<EventMilestone> Milestones { get; set; }
    [MemoryPackIgnore]
    private CharacterData charData;
    public void Init(CharacterData charData)
    {
        this.charData = charData;
        foreach (var milestone in Milestones)
        {
            milestone.Init(charData);
        }
    }
    public async Task<IEventResultData> HandleEvent(IEventRequestData data, AppDbContext db)
    {
        try
        {
            if (data is MilestoneClaim claim)
            {
                var id = claim.Id;
                var milestone = Milestones.FirstOrDefault(x => x.Id == id);
                if (milestone == null)
                {
                    throw new InvalidException("Mốc phần thưởng không tồn tại");
                }
                milestone.Claim(charData);
                return new MilestoneClaimResult()
                {
                    Success = true,
                    Rewards = milestone.Rewards
                };
            }

            throw new InvalidException("Không hợp lệ");
        }
        catch (InvalidException e)
        {
            return new MilestoneClaimResult()
            {
                Success = false,
                Message = e.Message
            };
        }
    }
}