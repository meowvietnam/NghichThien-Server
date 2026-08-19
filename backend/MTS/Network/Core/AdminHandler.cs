using MemoryPack;
using MTS.Exceptions;
using MTS.Game.Auction;
using MTS.Localization;
using MTS.Models;
using MTS.Network.Models;
using Network;
using Network.Extensions;

namespace MTS.Network.Core;

public class AdminHandler : IClientHandler
{
    private Client client;

    public AdminHandler(Client client)
    {
        this.client = client;
    }

    public void OnEnter()
    {
    }

    public async void Handle(Message<MessageTag> message)
    {
        var tag = message.Tag;
        var reader = message.Reader;
        if (tag == MessageTag.AddItems)
        {
            var items = reader.ReadArray<ItemRef>().ToList();
            if (!items.Any())
            {
                return;
            }

            var inventory = await client.GetInventory();
            RewardItems rewards;
            lock (client.CharacterLock)
            {
                rewards = inventory.AddToInventory(items);
            }

            client.Send(MessageTag.AddItemsResult, rewards);
        }
        else if (tag == MessageTag.SetInfinityEnergy)
        {
            var infinityEnergy = reader.Read<bool>();
            lock (client.CharacterLock)
            {
                client.CharacterData.SetIsInfiniteEnergy(infinityEnergy);
            }
        }
        else if (tag == MessageTag.SetNgoDaoDiem)
        {
            var ngoDaoDiem = reader.Read<int>();
            lock (client.CharacterLock)
            {
                client.CharacterData.Progression.SetNgoDaoDiem(ngoDaoDiem);
            }

            client.Send(MessageTag.SetNgoDaoDiemResult, ngoDaoDiem);
        }
        else if (tag == MessageTag.SetCamNgo)
        {
            var camNgo = reader.Read<CamNgo>();
            var inventory = await client.GetInventory();
            int newNgoDaoDiem;
            lock (client.CharacterLock)
            {
                newNgoDaoDiem = inventory.AddOrUpdate(camNgo);
            }

            var result = new SetCamNgoResult()
            {
                CamNgo = camNgo,
                NewNgoDaoDiem = newNgoDaoDiem
            };
            client.Send(MessageTag.SetCamNgoResult, result);
        }
        else if (tag == MessageTag.AddThanThong)
        {
            var id = reader.Read<ushort>();
            var inventory = await client.GetInventory();
            var equipped = false;
            lock (client.CharacterLock)
            {
                equipped = inventory.EquipThanThong(id);
            }

            if (equipped)
            {
                client.Send(MessageTag.AddThanThongResult, id);
            }
        }
        else if (tag == MessageTag.AddCongPhap)
        {
            var congPhap = reader.Read<CongPhap>();
            if (congPhap.Level == 0) return;
            lock (client.CharacterLock)
            {
                client.CharacterData.InventoryManager.AddOrUpdate(congPhap);
            }

            client.Send(MessageTag.AddCongPhapResult, congPhap);
        }
        else if (tag == MessageTag.SetMoneyVip)
        {
            var moneyVip = reader.Read<int>();
            lock (client.CharacterLock)
            {
                client.CharacterData.SetMoneyVip(moneyVip);
            }

            client.Send(MessageTag.SetMoneyVipResult, moneyVip);
        }
        else if (tag == MessageTag.SetMoney)
        {
            var money = reader.Read<int>();
            lock (client.CharacterLock)
            {
                client.CharacterData.Stats.SetLinhThach(money);
            }

            client.Send(MessageTag.SetMoneyResult, money);
        }
        else if (tag == MessageTag.SetExpRequest)
        {
            var exp = reader.Read<int>();
            int currentExp;
            lock (client.CharacterLock)
            {
                var progress = client.CharacterData.Progression;
                progress.SetCurrentExp(exp);
                currentExp = progress.CurrentExp;
            }

            client.Send(MessageTag.SetExpRequest, currentExp);
        }
        else if (tag == MessageTag.AdminNotify)
        {
            var msg = reader.Read();
            var charDatas = TcpServer.Instance.GetCharsOnline();
            foreach (var charData in charDatas)
            {
                charData?.Client.MemSend(MessageTag.Notify, msg);
            }
        }
        else if (tag == MessageTag.CreateNewAuctionSessionRequest)
        {
            Result result = null;
            try
            {
                var auctionManager = AuctionManager.Instance;
                if (auctionManager == null)
                {
                    throw new InvalidException(Strings.AuctionNotOpen);
                }

                var request = reader.MemRead<CreateNewAuctionSession>();
                result = auctionManager.CreateNewAuctionSession(request);
            }
            catch (InvalidException e)
            {
                result = new Result()
                {
                    Success = false,
                    Message = e.Message
                };
            }
            finally
            {
                client.MemSend(MessageTag.CreateNewAuctionSessionRequest, result);
            }
        }
        else if (tag == MessageTag.RemoveLastAuctionSessionRequest)
        {
            Result result = null;
            try
            {
                var auctionManager = AuctionManager.Instance;
                if (auctionManager == null)
                {
                    throw new InvalidException(Strings.AuctionNotOpen);
                }

                result = auctionManager.RemoveLastAuctionSession();
            }
            catch (InvalidException e)
            {
                result = new Result()
                {
                    Success = false,
                    Message = e.Message
                };
            }
            finally
            {
                client.MemSend(MessageTag.RemoveLastAuctionSessionRequest, result);
            }
        }
    }

    public void OnLeave()
    {
    }

    private static readonly MessageTag[] handler =
    [
        MessageTag.AddItems, MessageTag.SetInfinityEnergy, MessageTag.SetNgoDaoDiem, MessageTag.SetCamNgo,
        MessageTag.AddThanThong, MessageTag.AddCongPhap, MessageTag.SetMoneyVip, MessageTag.SetMoney,
        MessageTag.TestRequest,
        MessageTag.SetExpRequest, MessageTag.AdminNotify, MessageTag.CreateNewAuctionSessionRequest,
        MessageTag.RemoveLastAuctionSessionRequest
    ];

    public MessageTag[] HandleTag => handler;

    public void OnDisconnect(IProtocol<MessageTag> protocol)
    {
    }
}