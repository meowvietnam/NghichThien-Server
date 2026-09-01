using MTS.Exceptions;
using MTS.Localization;
using MTS.Models;
using MTS.Network;
using MTS.Network.ClientState;
using MTS.Network.Models;

namespace MTS.Game.Auction;

public class PlayerAuction : ISender<MessageTag>
{
    public PlayerAuctionModel? Model { get; private set; }
    private AuctionState state;
    public AuctionManager AuctionManager { get; private set; }
    public PlayerAuction(AuctionState state)
    {
        this.state = state;
    }
    public bool HasJoined()
    {
        if (Model == null)
        {
            return false;
        }
        if (AuctionManager == null)
        {
            return false;
        }
        return AuctionManager.Players.ContainsKey(Model.PlayerId);
    }
    public void Join(AuctionManager auctionManager)
    {
        AuctionManager = auctionManager;
        if (HasJoined())
        {
            throw new InvalidException(Strings.AuctionAlreadyJoined);
        }
        auctionManager.OnNewPlayerJoined(this);
    }

    public CharacterData GetCharacterData()
    {
        return state.GetCharacterData();
    }

    public Client GetClient()
    {
        return state.GetClient();
    }
    public void SetPlayerAuctionModel(PlayerAuctionModel model)
    {
        Model = model;
    }

    public void Exit()
    {
        if (HasJoined())
        {
            AuctionManager.PlayerExit(this);
        }
    }
    public void OnEnter()
    {
    }
    public void OnExit()
    {
        Exit();
    }
    public void Send(Span<byte> data)
    {
        state.Send(data);
    }

    public AuctionBidResult AuctionBid(int value)
    {
        return AuctionManager.PlayerBid(this, value);
    }
}
