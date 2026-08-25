using MTS.Models;
using MTS.Network;
using MTS.Network.ClientState;
using MTS.Network.Models;

namespace MTS.Game.DP;

public class DongPhuObserver
{
    public DongPhu DongPhu { get; set; }
    public HashSet<DongPhuState> States { get; set; } = new ();
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
            }
            DongPhuManager.OnRemoveState(this, state);
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

    public bool UpdateStateDailyReset(DongPhu dongPhu)
    {
        lock (stateLock)
        {
            foreach (var state in States)
            {
                if (state.IsMyDongPhu(dongPhu.CharId))
                {
                    state.OnResetDaily(dongPhu);
                    return true;
                }
            }
        }
        return false;
    }
    public void OnMyResetDaily(DongPhu dongPhu)
    {
        lock (stateLock)
        {
            
        }
    }

    public async Task SaveChanges()
    {
        await using var db = await Program.CreateDbAsync();
        lock (stateLock)
        {
            DongPhu.SaveChanges(db).GetAwaiter().GetResult();
            foreach (var dongPhuState in States)
            {
                dongPhuState.SaveChanges(db).GetAwaiter().GetResult();
            }
        }
        await db.SaveChangesAsync();
    }
}