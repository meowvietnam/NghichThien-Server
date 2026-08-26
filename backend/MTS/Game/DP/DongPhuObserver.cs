using MTS.Models;
using MTS.Network;
using MTS.Network.ClientState;
using MTS.Network.Models;
using static MTS.Game.Auction.AuctionManager;

namespace MTS.Game.DP;

public class DongPhuObserver
{
    public DongPhu DongPhu { get; private set; }
    public HashSet<DongPhuState> States { get; private set; } = new();
    private object stateLock = new();

    public DongPhuObserver(DongPhu dongPhu)
    {
        DongPhu = dongPhu;
    }
    public void SendAllIgnore<T>(DongPhuState state,MessageTag tag,T data)
    {
        lock(stateLock)
        {
            foreach (var item in States)
            {
                if(state != item)
                {
                    item.MemSend(tag, data);
                }
            }
        }
    }
    public void SendAll<T>(MessageTag tag, T data)
    {
        lock (stateLock)
        {
            foreach (var state in States)
            {
                state.MemSend(tag, data);
            }
        }
    }
    public void RemoveState(DongPhuState state)
    {
        lock (stateLock)
        {
            if (States.Remove(state))
            {
                state.OnRemove();
                DongPhuManager.OnRemoveState(this, state);
            }
        }
    }

    public void OnUserDeleted()
    {
        lock(stateLock)
        {
            foreach(var state in States)
            {
                state.ToAllMap();
            }
        }
    }

    public void OnDongPhuLiked(DongPhuLike like)
    {
    }

    public void OnWateringHelp(AddWateringHelp addWateringHelp)
    {
    }

    public void AddState(DongPhuState state)
    {
        lock (stateLock)
        {
            States.Add(state);
        }
    }

    public void OnMyResetDaily(DongPhu dongPhu)
    {
        lock (stateLock)
        {
        }
    }
}