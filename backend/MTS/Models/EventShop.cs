using MemoryPack;
using MTS.Data;
using MTS.Exceptions;
using MTS.IAP;
using MTS.Localization;
using MTS.Network.Models;
using MTS.Services;
using Newtonsoft.Json;
using Serilog;

namespace MTS.Models;

public class EventShopData
{
    public ushort Id { get; set; }
}

[MemoryPackable]
public partial class EventShop : IGameEvent
{
    [MemoryPackIgnore] private CharacterData charData;
    public string Title { get; set; }
    public List<ItemEventShop> Items { get; set; }

    public void Init(CharacterData charData)
    {
        this.charData = charData;
    }

    private ItemEventShop Validate(ushort id)
    {
        var item = Items.FirstOrDefault(x => x.Id == id);
        if (item == null)
        {
            throw new InvalidException("Vật phẩm không tồn tại");
        }

        if (item.HasBuy)
        {
            throw new InvalidException("Vật phẩm đã được mua");
        }

        return item;
    }

    public async Task<IEventResultData> HandleEvent(IEventRequestData eventRequestData, AppDbContext db)
    {
        if (eventRequestData is CreateOrderEventShop createOrder)
        {
            try
            {
                var id = createOrder.Id;
                var item = Validate(id);
                var data = new RechargeEvent1Data()
                {
                    Id = id
                };
                var link = await PaymentService.Instance.CreateLink(item.Amount, TimeSpan.FromHours(1));
                var expireTime = DateTimeOffset
                    .FromUnixTimeSeconds(link.ExpiredAt.Value)
                    .UtcDateTime;
                var order = new Order()
                {
                    ProductId = null,
                    CharId = charData.Id,
                    OrderType = OrderType.EventShop,
                    Amount = item.Amount,
                    Link = link.CheckoutUrl,
                    OrderId = link.OrderCode,
                    ExpireAt = expireTime,
                    Data = JsonConvert.SerializeObject(data)
                };
                await db.Orders.AddAsync(order);
                await db.SaveChangesAsync();
                return new CreateOrderEventShopResult()
                {
                    Success = true,
                    Link = link.CheckoutUrl
                };
            }
            catch (InvalidException e)
            {
                return new CreateOrderEventShopResult()
                {
                    Success = false,
                    Message = e.Message
                };
            }
        }

        if (eventRequestData is HandleOrderEventShop handle)
        {
            try
            {
                var order = handle.Order;
                Log.Information("{Name} vừa nạp sự kiện {Amount} VNĐ", charData.Name,
                    Utils.Utils.FormatCount(order.Amount));
                var data = JsonConvert.DeserializeObject<EventShopData>(order.Data);
                var id = data.Id;
                var item = Validate(id);
                order.Status = OrderStatus.Complete;
                var purchase = new IapPurchase()
                {
                    Store = Store.Web,
                    ProductId = null,
                    CharId = order.CharId,
                    AmountSnapshot = order.Amount,
                    PurchaseTokenHash = Array.Empty<byte>()
                };
                await db.IapPurchases.AddAsync(purchase);
                item.HasBuy = false;
                var mail = CreateMail(item, includeRechargeBonus: true);
                var mailManager = new MailManager(charData, charData.Client);
                await mailManager.SendMail(mail, db, true);
                return new HandleOrderEventShopResult()
                {
                    Success = true
                };
            }
            catch (InvalidException e)
            {
                return new HandleOrderEventShopResult()
                {
                    Success = false,
                    Message = e.Message
                };
            }
        }

        if (eventRequestData is BuyItemEventShop request)
        {
            try
            {
                var id = request.Id;
                var item = Validate(id);
                var remaining = charData.MoneyVip - item.MoneyVip;
                if (remaining < 0)
                {
                    throw new InvalidException(Strings.NotEnoughMoneyVip);
                }
                item.HasBuy = false;
                var mail = CreateMail(item, includeRechargeBonus: false);
                var mailManager = new MailManager(charData, charData.Client);
                await mailManager.SendMail(mail, db, true);
                charData.SetMoneyVip(remaining);
                charData.AddSpendMoneyVipOnline(item.MoneyVip, SpendFor.EventShop);
                return new BuyItemEventShopResult()
                {
                    Success = true,
                    Message = "Mua vật phẩm thành công"
                };
            }
            catch (InvalidException e)
            {
                return new BuyItemEventShopResult()
                {
                    Success = false,
                    Message = e.Message
                };
            }
        }

        return null;
    }

    public Mail CreateMail(ItemEventShop item, bool includeRechargeBonus)
    {
        var rewards = item.GetRewards();
        if (includeRechargeBonus && item.BonusMoneyVip > 0)
        {
            rewards.Add(new MailRewardMoneyVip
            {
                MoneyVip = item.BonusMoneyVip
            });
        }

        var mail = new Mail()
        {
            CharId = charData.Id,
            Title = "Phần thưởng nạp tiền",
            Content = $"Chúc mừng! Bạn nhận được phần thưởng từ cửa hàng.",
            Rewards = rewards
        };
        return mail;
    }
}
