using MTS.Models;
using MTS.Network;
using MTS.Network.ClientState;
using MTS.Network.Models;

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