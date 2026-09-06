using MTS.Exceptions;
using MTS.Game;
using MTS.Network.Models;
using Network;
using Network.Extensions;

namespace MTS.Network.ClientState;

public class AllMaps : IClientState
{
    private Client client;
    private const string NotEnoughEnergy = "Không đủ năng lượng";
    private bool isMoving;
    private static event Action<PlayerMoving> OnPlayerMoving;
    public AllMaps(Client client)
    {
        this.client = client;
        client.OnLogout += OnLogout;
        client.OnOtherClientLogin += OnOtherClientLogin;
        client.OnRemoveDisconnectedSession += OnRemoveDisconnectedSession;
        OnPlayerMoving += OnOtherPlayerMoving;
    }

    private void OnOtherPlayerMoving(PlayerMoving playerMoving)
    {
        if (IsMe(playerMoving.CharId))
        {
            return;
        }
        client.MemSend(MessageTag.OtherPlayerMoving, playerMoving);
    }

    public void OnRemove()
    {
        client.OnOtherClientLogin -= OnOtherClientLogin;
        client.OnLogout -= OnLogout;
        client.OnRemoveDisconnectedSession -= OnRemoveDisconnectedSession;
        OnPlayerMoving -= OnOtherPlayerMoving;
    }
    private void OnRemoveDisconnectedSession(Client client)
    {
        OnRemove();
    }

    private void OnOtherClientLogin(Client client)
    {
        OnRemove();
    }

    private void OnLogout(Client client)
    {
        OnRemove();
    }

    private bool IsMe(int charId)
    {
        var charData = client.CharacterData;
        if (charData == null)
        {
            return false;
        }
        return charData.Id == charId;
    }
    public void OnEnter()
    {
    }

    private CharacterSnapshot[] GetCharsOnlineThisNode()
    {
        int currentNodeId;
        int charId;
        lock (client.CharacterLock)
        {
            var charData = client.CharacterData;
            currentNodeId = charData.Progression.IdCurrentNode;
            charId = charData.Id;
        }

        var charsOnline = TcpServer.Instance.GetCharsOnline()
            .Where(x => x.Progression.IdCurrentNode == currentNodeId && x.Id != charId)
            .Select(x => new CharacterSnapshot(x))
            .OrderBy(_ => Random.Shared.Next())
            .Take(5)
            .ToArray();
        charsOnline = new[] { new CharacterSnapshot(TcpServer.Instance.Scarecrow) }.Concat(charsOnline).ToArray();
        return charsOnline;
    }

    public void Handle(Message<MessageTag> message)
    {
        var tag = message.Tag;
        var reader = message.Reader;
        if (tag == MessageTag.MoveToMap)
        {
            lock (client.StateLock)
            {
                if (isMoving)
                {
                    return;
                }

                isMoving = true;
            }

            var id = reader.Read<int>();
            try
            {
                MoveToMapResult result;
                lock (client.CharacterLock)
                {
                    var charData = client.CharacterData;
                    var currentMapId = charData.Progression.IdCurrentNode;
                    var path = GameConfig.MapManager.FindPath(currentMapId, id);
                    var energy = charData.IsInfiniteEnergy ? 0 : path.Count;
                    var newEnergy = charData.Progression.CurrentEnergy - energy;
                    if (newEnergy < 0)
                    {
                        throw new InvalidException($"{NotEnoughEnergy}: {energy}");
                    }

                    var stats = charData.Stats;
                    var dayUsed = (int)Math.Round(32f * path.Count / stats.TocDoBay, MidpointRounding.AwayFromZero);
                    var newAge = stats.TuoiTac + new TimeSpan(dayUsed, 0, 0, 0);
                    stats.SetTuoiTac(newAge);
                    charData.Progression.SetIdCurrentNode(id);
                    charData.Progression.SetCurrentEnergy(newEnergy);
                    var charsOnline = GetCharsOnlineThisNode();
                    var pathIds = path.Select(x => x.Id).ToArray();
                    result = new MoveToMapResult()
                    {
                        Success = true,
                        Energy = newEnergy,
                        Path = pathIds,
                        CharactersOnline = charsOnline,
                        NewAge = newAge
                    };
                    var playerMoving = new PlayerMoving()
                    {
                        CharId = charData.Id,
                        Name = charData.Name,
                        Appearance = charData.Appearance,
                        Gender = charData.Gender,
                        IsSwordRiding = charData.IsInfiniteEnergy,
                        Path = pathIds
                    };
                    OnPlayerMoving?.Invoke(playerMoving);
                }

                client.Send(MessageTag.MoveToMapResult, result);
                
            }
            catch (InvalidException e)
            {
                var result = new MoveToMapResult()
                {
                    Success = false,
                    Message = e.Message
                };
                client.Send(MessageTag.MoveToMapResult, result);
            }
            finally
            {
                lock (client.StateLock)
                {
                    isMoving = false;
                }
            }
        }
        else if (tag == MessageTag.GetPlayerOnlineThisNode)
        {
            try
            {
                var charsOnline = GetCharsOnlineThisNode();
                var result = new GetCharacterOnlineThisNodeResult()
                {
                    Success = true,
                    CharactersOnline = charsOnline
                };
                client.Send(MessageTag.GetPlayerOnlineThisNodeResult, result);
            }
            catch (InvalidException e)
            {
                var result = new GetCharacterOnlineThisNodeResult()
                {
                    Success = false,
                    Message = e.Message
                };
                client.Send(MessageTag.GetPlayerOnlineThisNodeResult, result);
            }
        }
        else if (tag == MessageTag.WatchAdsForEnergy)
        {
            WatchAdsForEnergyResult result = null;
            try
            {
                lock (client.CharacterLock)
                {
                    var charData = client.CharacterData;
                    charData.Progression.SetCurrentEnergy(100);
                    result = new WatchAdsForEnergyResult
                    {
                        Success = true,
                        NewEnergy = 100
                    };
                }
            }
            catch (InvalidException e)
            {
                result = new WatchAdsForEnergyResult()
                {
                    Success = false,
                    Message = e.Message
                };
            }
            finally
            {
                client.Send(MessageTag.WatchAdsForEnergyResult, result);
            }
        }
    }

    public void OnDisconnect(IProtocol<MessageTag> protocol)
    {
    }

    public void OnLeave()
    {
        OnRemove();
    }

    public MessageState State => MessageState.AllMaps;
}