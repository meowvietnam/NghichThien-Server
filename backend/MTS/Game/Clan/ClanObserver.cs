using MTS.Network;
using MTS.Network.Models;

namespace MTS.Game.Clan;

public interface IClanObserverState
{
    int CharId { get; }
    void OnClanAddMember(Models.ClanMember member);
    void OnClanRemoveMember(int clanId, int charId, ClanRemoveMemberReason reason);
    void OnClanContribution(ClanContribution contribution);
    void MemSend<T>(MessageTag tag, T data);
    void OnClanDeleted(int clanId);
}

public sealed class ClanObserver
{
    private readonly object stateLock = new();

    public ClanObserver(Models.Clan clan)
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

    public void SendAll<T>(MessageTag tag, T data)
    {
        lock (stateLock)
        {
            foreach (var state in States.ToArray())
            {
                state.MemSend(tag, data);
            }
        }
    }

    public void SendAllIgnore<T>(IClanObserverState ignore, MessageTag tag, T data)
    {
        lock (stateLock)
        {
            foreach (var state in States.ToArray())
            {
                if (!ReferenceEquals(state, ignore))
                {
                    state.MemSend(tag, data);
                }
            }
        }
    }


    public void NotifyAddMember(Models.ClanMember member)
    {
        lock (stateLock)
        {
            foreach (var state in States.ToArray())
            {
                state.OnClanAddMember(member);
            }
        }
    }

    public void NotifyRemoveMember(int charId, ClanRemoveMemberReason reason)
    {
        lock (stateLock)
        {
            foreach (var state in States.ToArray())
            {
                state.OnClanRemoveMember(Clan.Id, charId, reason);
            }
        }
    }

    public void NotifyContribution(ClanContribution contribution)
    {
        lock (stateLock)
        {
            foreach (var state in States.ToArray())
            {
                state.OnClanContribution(contribution);
            }
        }
    }

    public void OnClanDeleted()
    {
        lock (stateLock)
        {
            foreach (var state in States.ToArray())
            {
                state.OnClanDeleted(Clan.Id);
            }
            States.Clear();
        }
    }
}
