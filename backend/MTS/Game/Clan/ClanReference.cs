namespace MTS.Game.Clan;

public sealed class ClanReference
{
    private readonly object stateLock = new();

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
                ClanManager.OnRemoveState(this, state);
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
}
