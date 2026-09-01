using MemoryPack;
using MTS.Exceptions;
using MTS.Models;

namespace MTS.Network.Models;
[MemoryPackable]
public partial class EventMilestone
{
    [MemoryPackIgnore] private EventInventory inventory;
    public ushort Id { get; set; }
    public ItemEvent[] Targets { get; set; }
    public List<IEventReward> Rewards { get; set; }
    public bool IsClaim { get; set; }
    public int[] Progresses => Targets.Select(x => inventory?.CountOf(x.Id) ?? 0).ToArray();
    public void Init(CharacterData charData)
    {
        inventory = charData.EventManager.Inventory;
    }

    public bool IsCompleted()
    {
        var progresses = Progresses;
        for (int i = 0; i < progresses.Length; i++)
        {
            var progress = progresses[i];
            var target = Targets[i];
            if (progress < target.Count)
            {
                return false;
            }
        }
        return true;
    }
    public void Claim(CharacterData charData)
    {
        if (IsClaim)
        {
            throw new InvalidException("Bạn đã nhận mốc này");
        }
        if (!IsCompleted())
        {
            throw new InvalidException("Chưa hoàn thành");
        }
        foreach (var reward in Rewards)
        {
            reward.Claim(charData);
        }
        IsClaim = true;
    }
}