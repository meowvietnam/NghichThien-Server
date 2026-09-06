using Microsoft.EntityFrameworkCore;
using MTS.Data;
using MTS.Exceptions;
using MTS.Models;
using MTS.Network;
using MTS.Network.Models;

namespace MTS.Services;

public static class EventShopService
{
    public static async Task Handle(Order order, AppDbContext db)
    {
        var charData = TcpServer.Instance.GetCharsOnline().FirstOrDefault(x => x.Id == order.CharId);
        if (charData == null)
        {
            charData = await db.Characters
                .Include(x => x.EventManager)
                .ThenInclude(x => x.Inventory)
                .ThenInclude(x => x.Items)
                .Include(characterData => characterData.EventManager)
                .ThenInclude(eventManager => eventManager.Events)
                .FirstOrDefaultAsync(x => x.Id == order.CharId);
            if (charData == null)
            {
                throw new InvalidException("Nhân vật không tồn tại");
            }
            charData.EventManager.Init(charData);
        }
        var thisEvent = charData.EventManager.Events.FirstOrDefault(x => x.GameEvent is EventShop);
        if (thisEvent == null)
        {
            throw new InvalidException("Sự kiện không tồn tại");
        }
        if (!thisEvent.IsInEvent())
        {
            throw new InvalidException("Sự kiện không diễn ra");
        }

        var data = new HandleOrderRechargeEvent1()
        {
            Order = order
        };
        var result = await thisEvent.Handle(data, db);
        if (!result.IsSuccess())
        {
            throw new InvalidException("Xử lý thất bại");
        }
        await thisEvent.SaveChanges(db);
        await db.SaveChangesAsync();
    }
}