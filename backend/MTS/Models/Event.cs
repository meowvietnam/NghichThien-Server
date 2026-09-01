using System.ComponentModel.DataAnnotations.Schema;
using MemoryPack;
using Microsoft.EntityFrameworkCore;
using MTS.Data;
using MTS.Network.Models;

namespace MTS.Models;
[MemoryPackable]
public partial class Event
{
    public int CharId { get; set; }
    public ushort Id { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public IGameEvent GameEvent { get; set; }
    [NotMapped]
    [MemoryPackIgnore]
    private bool dirty;
    public bool IsInEvent()
    {
        var now = DateTime.UtcNow;
        return now > StartAt &&  now < EndAt;
    }

    public async Task<IEventResultData> Handle(IEventRequestData data, AppDbContext db)
    {
        var result = await GameEvent.HandleEvent(data, db);
        if (result.IsSuccess())
        {
            dirty = true;
        }
        return result;
    }
    public async Task SaveChanges(AppDbContext db)
    {
        if (!dirty)
        {
            return;
        }
        await db.Events
            .Where(x => x.CharId == CharId && x.Id == Id)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.GameEvent, GameEvent));
        dirty = false;
    }
}