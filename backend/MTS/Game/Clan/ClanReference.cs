using MTS.Models;
using Serilog;

namespace MTS.Game.Clan;

public sealed class ClanReference
{
    private readonly object stateLock = new();
    private readonly SemaphoreSlim saveLock = new(1, 1);

    public ClanReference(Models.Clan clan)
    {
        Clan = clan;
    }

    public Models.Clan Clan { get; }
    public HashSet<IClanObserverState> States { get; } = new();

    public void AddState(IClanObserverState state)
    {
        lock (stateLock)
        {
            States.Add(state);
        }
    }

    public void RemoveState(IClanObserverState state)
    {
        lock (stateLock)
        {
            if (States.Remove(state))
            {
                _ = ClanManager.OnRemoveState(this, state);
            }
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
            Clan.SaveChanges(db);
            await db.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error saving Clan {ClanId}", Clan.Id);
            return false;
        }
        finally
        {
            saveLock.Release();
        }
    }
}
