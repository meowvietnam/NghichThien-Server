using MemoryPack;
using MTS.Data;
using MTS.Network.Models;

namespace MTS.Models;
[MemoryPackable]
[MemoryPackUnion(0, typeof(MilestoneEvent))]
[MemoryPackUnion(1, typeof(RechargeEvent1))]
public partial interface IGameEvent
{
    void Init(CharacterData charData);
    Task<IEventResultData> HandleEvent(IEventRequestData eventRequestData, AppDbContext db);
}