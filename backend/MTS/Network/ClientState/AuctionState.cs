using MTS.Exceptions;
using MTS.Game.Auction;
using MTS.Models;
using MTS.Network.Models;

namespace MTS.Network.ClientState;

public class AuctionState : IClientState, ISender<MessageTag>
{
    private PlayerAuction playerAuction;
    private readonly Client client;
    public AuctionState(Client client)
    {
        this.client = client;
        playerAuction = new PlayerAuction(this);
        client.OnOtherClientLogin += OnOtherClientLogin;
        client.OnLogout += OnLogout;
        client.OnRemoveDisconnectedSession += OnRemoveDisconnectedSession;
    }
    private void OnRemoveDisconnectedSession(Client client)
    {
        playerAuction.Exit();
    }
    private void OnLogout(Client client)
    {
        playerAuction.Exit();
    }
    private void OnOtherClientLogin(Client client)
    {
        playerAuction.Exit();
    }

    public void OnEnter()
    {
        playerAuction.OnEnter();
    }

    public void Handle(Message<MessageTag> message)
    {
        var tag = message.Tag;
        var reader = message.Reader;
        if (tag == MessageTag.JoinAuction)
        {
            JoinAuctionResult result = null!;
            try
            {
                var auctionManger = AuctionManager.Instance;
                playerAuction.Join(auctionManger);
                var auctionData = new AuctionData()
                {
                    CurrentItem = auctionManger.GetCurrentAuctionItem(),
                    Players = auctionManger.PlayerModels,
                    MyPlayer = playerAuction.Model,
                    MyMoneyVip = GetCharacterData().MoneyVip
                };
                result = new JoinAuctionResult()
                {
                    Success = true,
                    AuctionData = auctionData,
                };
            }
            catch (InvalidException e)
            {
                result = new JoinAuctionResult()
                {
                    Success = false,
                    Message = e.Message
                };
            }
            finally
            {
                client.MemSend(MessageTag.JoinAuctionResult, result);
            }
        }
        else if (tag == MessageTag.AuctionBid)
        {
            var value = reader.Read<int>();
            AuctionBidResult result = null!;
            try
            {
                result = playerAuction.AuctionBid(value);
            }
            catch (InvalidException e)
            {
                result = new AuctionBidResult()
                {
                    Success = false,
                    Message = e.Message
                };
            }
            finally
            {
                client.MemSend(MessageTag.AuctionBidResult, result);
            }
        }
        else if (tag == MessageTag.GetAuctionInfo)
        {
            var auctionManager = playerAuction.AuctionManager;
            var auctionInfo = auctionManager.GetInfo();
            var result = new GetAuctionInfoResult()
            {
                Success = true,
                AuctionInfo = auctionInfo,
            };
            client.MemSend(MessageTag.GetAuctionInfoResult, result);
        }
    }

    public void OnLeave()
    {
        playerAuction.OnExit();
        client.OnOtherClientLogin -= OnOtherClientLogin;
        client.OnLogout -= OnLogout;
        client.OnRemoveDisconnectedSession -= OnRemoveDisconnectedSession;
    }
    public void OnDisconnect(IProtocol<MessageTag> protocol)
    {
    }

    public void Send(Span<byte> data)
    {
        client.Send(data);
    }
    public MessageState State => MessageState.AuctionState;

    public CharacterData GetCharacterData()
    {
        return client.CharacterData;
    }

    public Client GetClient()
    {
        return client;
    }
}