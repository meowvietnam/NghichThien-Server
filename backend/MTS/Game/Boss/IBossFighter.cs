using MTS.Models;
using MTS.Network;

namespace MTS.Game.Boss;

public interface IBossFighter : ISender<MessageTag>
{
    int Id { get; }
    bool IsConnected { get; }
    CharacterData CharData { get; }
    object MainLock { get; }
}