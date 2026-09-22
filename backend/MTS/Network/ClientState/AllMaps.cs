using Amazon.S3.Model;
using MTS.Exceptions;
using MTS.Game;
using MTS.Game.Boss;
using MTS.Network.Models;
using Network;
using Network.Extensions;

namespace MTS.Network.ClientState;

public class AllMaps : IClientState
{
    private Client client;
    private const string NotEnoughEnergy = "Không đủ năng lượng";
    private bool isMoving;
    private InvitePvp? currentInvited;
    private static event Action<PlayerMoving> OnPlayerMoving;

    public AllMaps(Client client)
    {
        this.client = client;
        client.OnLogout += OnLogout;
        client.OnOtherClientLogin += OnOtherClientLogin;
        client.OnDisconnectedSessionExpired += OnDisconnectedSessionExpired;
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
        client.OnDisconnectedSessionExpired -= OnDisconnectedSessionExpired;
        OnPlayerMoving -= OnOtherPlayerMoving;
    }

    private void OnDisconnectedSessionExpired(Client client)
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
        if (tag == MessageTag.GetBossStatus)
        {
            var nodeId = reader.MemRead<int>();
            var boss = BossManager.Instance;
            var status = boss != null && (nodeId == 0 ||
                         boss.NodeId == nodeId && client.CharacterData.Progression.IdCurrentNode == nodeId)
                ? boss.GetStatus(client.CharacterData.Id)
                : new BossStatus { NodeId = nodeId };
            status.RequestNodeId = nodeId;
            client.MemSend(MessageTag.BossStatus, status);
        }
        else if (tag == MessageTag.JoinBoss)
        {
            var request = reader.MemRead<JoinBossRequest>();
            var result = new JoinBossResult();
            try
            {
                lock (client.StateLock)
                {
                    if (isMoving || !ReferenceEquals(client.ClientState, this))
                        throw new InvalidException("Hãy dừng tại vị trí boss trước khi tham gia.");
                    var boss = BossManager.Instance ?? throw new InvalidException("Boss chưa xuất hiện.");
                    var state = new BossState(client, boss);
                    boss.Join(state, request);
                    client.ChangeState(state);
                    result.Success = true;
                    result.Status = boss.GetStatus(client.CharacterData.Id);
                    result.Player = new CharacterSnapshot(client.CharacterData);
                }
            }
            catch (InvalidException e) { result.Message = e.Message; }
            client.MemSend(MessageTag.JoinBossResult, result);
        }
        else if (tag == MessageTag.MoveToMap)
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
                    charData.SetTuoiTacSpecial(newAge);
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
                        NewAge = charData.Stats.TuoiTac
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
        else if (tag == MessageTag.InvitePvpRequest)
        {
            var request = reader.MemRead<InvitePvpRequest>();
            InvitePvpResult result = null;
            try
            {
                lock (client.CharacterLock)
                {
                    var state = TryGetPlayerState(request.Defender);
                    result = TryInvitePvp(state);
                }
            }
            catch (InvalidException ex)
            {
                result = new InvitePvpResult()
                {
                    Success = false,
                    Message = ex.Message
                };
            }
            finally
            {
                client.MemSend(MessageTag.InvitePvpRequest, result);
            }
        }
        else if (tag == MessageTag.InvitePvpResponse)
        {
            var response = reader.MemRead<InvitePvpResponse>();
            lock (client.CharacterLock)
            {
                OnInviteResponse(response);
            }
        }
    }

    private void OnInviteResponse(InvitePvpResponse response)
    {
        if (response.Accept)
        {
            try
            {
                if (currentInvited == null)
                {
                    throw new InvalidException("Chưa có lời mời");
                }

                if (currentInvited.IsExpire())
                {
                    throw new InvalidException("Lời mời hết hạn");
                }

                var otherState = TryGetPlayerState(currentInvited.Inviter);
                var thisChar = client.CharacterData;
                var otherChar = otherState.client.CharacterData;
                if (thisChar == null || otherChar == null)
                {
                    throw new InvalidException("Không thể mời người chơi này");
                }

                var pvpMatched = new PvpMatched()
                {
                    Inviter = new CharacterSnapshot(otherChar),
                    Defender = new CharacterSnapshot(thisChar)
                };
                var myPvpState = new CombatPvp(client, thisChar);
                var otherPvpState = new CombatPvp(otherState.client, otherChar);
                myPvpState.SetEnemy(otherPvpState);
                otherPvpState.SetEnemy(myPvpState);
                var manager = new CombatPvpManager(myPvpState, otherPvpState);
                myPvpState.SetManager(manager);
                otherPvpState.SetManager(manager);
                client.ChangeState(myPvpState);
                otherState.client.ChangeState(otherPvpState);
                client.MemSend(MessageTag.PvpMatched, pvpMatched);
                otherState.client.MemSend(MessageTag.PvpMatched, pvpMatched);
            }
            catch (InvalidException e)
            {
                client.MemSend(MessageTag.Notify, e.Message);
            }
        }
        else
        {
            if (currentInvited == null) return;
            try
            {
                var state = TryGetPlayerState(currentInvited.Inviter);
                state.currentInvited = null;
                state.client.MemSend(MessageTag.Notify, $"{client.CharacterData?.Name} đã từ chối lời mời thách đấu");
                currentInvited = null;
            }
            catch (InvalidException e)
            {
            }
        }
    }

    private AllMaps TryGetPlayerState(int charId)
    {
        var charData = TcpServer.Instance.GetCharsOnline().FirstOrDefault(x => x.Id == charId);
        if (charData == null)
        {
            throw new InvalidException("Người chơi không online");
        }

        var state = charData.Client.ClientState as AllMaps;
        if (state is null)
        {
            throw new InvalidException("Trạng thái không phù hợp");
        }

        return state;
    }

    private void EnsureCurrentInvitedIsExpire()
    {
        if (currentInvited != null)
        {
            if (!currentInvited.IsExpire())
            {
                throw new InvalidException("Lời mời chưa hết hạn");
            }
        }
    }

    public InvitePvpResult TryInvitePvp(AllMaps otherPlayerState)
    {
        EnsureCurrentInvitedIsExpire();
        otherPlayerState.EnsureCurrentInvitedIsExpire();
        var thisChar = client.CharacterData;
        var otherChar = otherPlayerState.client.CharacterData;
        if (thisChar == null || otherChar == null)
        {
            throw new InvalidException("Không thể mời người chơi này");
        }

        var invitePvp = new InvitePvp()
        {
            Inviter = thisChar.Id,
            Defender = otherChar.Id,
            InviterName = thisChar.Name,
            DefenderName = otherChar.Name,
            Expire = DateTime.UtcNow.AddSeconds(30)
        };
        currentInvited = invitePvp;
        otherPlayerState.currentInvited = invitePvp;
        otherPlayerState.client.MemSend(MessageTag.InvitePvp, invitePvp);
        return new InvitePvpResult()
        {
            Success = true,
            InvitePvp = invitePvp,
            Message = $"Gửi lời mời thách đấu đến {invitePvp.DefenderName} thành công"
        };
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
