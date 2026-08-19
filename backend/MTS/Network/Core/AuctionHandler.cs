using MTS.Exceptions;
using MTS.Game.Auction;
using MTS.Localization;
using MTS.Network.Models;

namespace MTS.Network.Core;

public class AuctionHandler : IClientHandler
{
    private readonly Client client;
    public AuctionHandler(Client client)
    {
        this.client = client;
    }
    public void OnEnter()
    {
    }

    public void Handle(Message<MessageTag> message)
    {
        var tag = message.Tag;
        var reader = message.Reader;
        if (tag == MessageTag.GetAuctionInfo2Request)
        {
            GetAuctionInfoResult result = null!;
            try
            {
                var auctionManager = AuctionManager.Instance;
                if (auctionManager == null)
                {
                    throw new InvalidException(Strings.AuctionNotOpen);
                }
                
                var auctionInfo = auctionManager.GetInfo();
                result = new GetAuctionInfoResult()
                {
                    Success = true,
                    AuctionInfo = auctionInfo,
                };
            }
            catch (InvalidException e)
            {
                result = new GetAuctionInfoResult()
                {
                    Success = false,
                    Message = e.Message
                };
            }
            finally
            {
                client.MemSend(MessageTag.GetAuctionInfo2Request, result);
            }
        }
    }

    public void OnLeave()
    {
    }

    private static MessageTag[] handler = [MessageTag.GetAuctionInfo2Request];
    public MessageTag[] HandleTag => handler;

    public void OnDisconnect(IProtocol<MessageTag> protocol)
    {
    }
}