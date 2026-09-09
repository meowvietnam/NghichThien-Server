using MTS.Models;
using MTS.Network.ClientState;
using Serilog;

namespace MTS.Game.DP;

public class DongPhuReference
{
    public DongPhu DongPhu { get; private set; }
    public HashSet<DongPhuState> States { get; private set; } = new();
    private object stateLock = new();
    private SemaphoreSlim saveLock = new(1, 1);

    public DongPhuReference(DongPhu dongPhu)
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

    public bool HasNoStates()
    {
        lock (stateLock)
        {
            return States.Count == 0;
        }
    }

    public async Task<bool> SaveChanges()
    {
        await saveLock.WaitAsync();
        try
        {
            await using var db = await Program.CreateDbAsync();
            DongPhu.SaveChanges(db);
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error saving changes DongPhu");
            return false;
        }
        finally
        {
            saveLock.Release();
        }
    }
}
