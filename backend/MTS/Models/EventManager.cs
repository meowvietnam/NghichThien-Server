using MTS.Data;
using MTS.Exceptions;
using MTS.Network.Models;

namespace MTS.Models;

public class EventManager
{
    public int CharId { get; set; }
    public EventInventory Inventory { get; set; } = new();
    public List<Event> Events { get; set; } = new();

    public void Init(CharacterData charData)
    {
        foreach (var @event in Events)
        {
            @event.GameEvent.Init(charData);
        }
    }
    public bool IsDefault()
    {
        return CharId == 0;
    }
    public List<Event> GetActiveEvents()
    {
        return Events.Where(x => x.IsInEvent()).ToList();
    }

    public Event GetEvent(ushort id)
    {
        var @event = Events.FirstOrDefault(x => x.Id == id);
        if (@event == null)
        {
            throw new InvalidException("Sự kiện không tồn tại");
        }
        if (!@event.IsInEvent())
        {
            throw new InvalidException("Sự kiện không diễn ra");
        }
        return @event;
    }
    public async Task<IEventResultData> HandleEvent(ushort id, IEventRequestData data, AppDbContext db)
    {
        var @event = GetEvent(id);
        return await @event.Handle(data, db);
    }
    public async Task SaveChanges(AppDbContext db)
    {
        Inventory.SaveChanges(db);
        foreach (var @event in Events)
        {
            await @event.SaveChanges(db);
        }
    }

    public void AcceptChanges()
    {
        Inventory.AcceptChanges();
    }
}
