using MTS.Models;
using MTS.Network.ClientState;
using Serilog;

namespace MTS.Game.DP;

public class DongPhuReference
{
    public DongPhu DongPhu { get; private set; }
    public HashSet<DongPhuState> States { get; private set; } = new();
    private object stateLock = new();
    private CancellationTokenSource cts = new();

    public DongPhuReference(DongPhu dongPhu)
    {
        DongPhu = dongPhu;
    }

    public CancellationTokenSource GetCancelToken()
    {
        return cts;
    }

    public CancellationTokenSource ResetCancelToken()
    {
        cts = new CancellationTokenSource();
        return cts;
    }

    public void RemoveState(DongPhuState state)
    {
        lock (stateLock)
        {
            if (States.Remove(state))
            {
                state.OnRemove();
                _ = DongPhuManager.OnRemoveState(this, state);
            }
        }
    }

    public void AddState(DongPhuState state)
    {
        lock (stateLock)
        {
            States.Add(state);
        }
    }

    public async Task SaveChanges()
    {
        try
        {
            await using var db = await Program.CreateDbAsync();
            await DongPhu.SaveChanges(db);
            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error saving changes DongPhu");
        }
    }
}