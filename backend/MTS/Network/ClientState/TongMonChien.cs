using MTS.Exceptions;
using MTS.Game.Clan;
using MTS.Game.TMC;
using MTS.Models;
using MTS.Network.Models;
using Serilog;

namespace MTS.Network.ClientState;

/// <summary>
/// State khi người chơi mở scene Tông môn chiến. Mọi luật chiến nằm trong <see cref="WarManager"/>;
/// state này chỉ xác thực người gửi, chuyển tiếp lệnh và nhận thông báo (delta, ghép trận, kick).
/// </summary>
public class TongMonChien : IClientState, IWarSession
{
    private readonly Client client;
    private bool bound;
    private bool left;

    public TongMonChien(Client client)
    {
        this.client = client;
    }

    public int CharId => client.CharacterData?.Id ?? 0;
    public Client Client => client;

    public void OnEnter()
    {
        Bind();
        // Không gọi WarManager đồng bộ ở đây: SceneManager đang giữ client.StateLock.
        _ = Task.Run(EnterWar);
    }

    private void Bind()
    {
        if (bound) return;
        bound = true;
        client.OnLogout += OnClientGone;
        client.OnOtherClientLogin += OnClientGone;
        client.OnDisconnectedSessionExpired += OnClientGone;
        client.OnSessionReconnected += OnReconnected;
    }

    private void Unbind()
    {
        if (!bound) return;
        bound = false;
        client.OnLogout -= OnClientGone;
        client.OnOtherClientLogin -= OnClientGone;
        client.OnDisconnectedSessionExpired -= OnClientGone;
        client.OnSessionReconnected -= OnReconnected;
    }

    private void OnClientGone(Client _)
    {
        left = true;
        Unbind();
        WarManager.Leave(this);
    }

    private void OnReconnected(Client _)
    {
        if (left) return;
        Task.Run(EnterWar);
    }

    private async Task<(Clan clan, ClanRole role)> GetClan()
    {
        var reference = await ClanManager.FindReferenceByMember(CharId);
        var clan = reference.Clan;
        var member = clan.Members.FirstOrDefault(x => x.CharId == CharId)
                     ?? throw new InvalidException("Chưa gia nhập tông môn");
        var role = clan.LeaderCharId == CharId ? ClanRole.Leader : member.Role;
        return (clan, role);
    }

    private async Task EnterWar()
    {
        try
        {
            var (clan, role) = await GetClan();
            if (left) return;
            var charData = client.CharacterData ?? throw new InvalidException("Nhân vật không tồn tại");
            var snapshot = WarManager.Enter(this, charData, clan, role);
            MemSend(MessageTag.WarSnapshot, snapshot);
        }
        catch (InvalidException e)
        {
            MemSend(MessageTag.WarSnapshot, new WarSnapshot { Success = false, Message = e.Message });
        }
        catch (Exception e)
        {
            Log.Error(e, "TongMonChien enter failed; CharId={CharId}", CharId);
            MemSend(MessageTag.WarSnapshot, new WarSnapshot { Success = false, Message = "Không thể vào Tông môn chiến" });
        }
    }

    public async void Handle(Message<MessageTag> message)
    {
        var tag = message.Tag;
        var reader = message.Reader;
        try
        {
            switch (tag)
            {
                case MessageTag.WarGetSnapshot:
                {
                    reader.MemRead<WarGetSnapshotRequest>();
                    WarSnapshot snapshot;
                    try
                    {
                        var (clan, role) = await GetClan();
                        snapshot = WarManager.GetSnapshot(CharId, clan, role);
                    }
                    catch (InvalidException e)
                    {
                        snapshot = new WarSnapshot { Success = false, Message = e.Message };
                    }

                    MemSend(MessageTag.WarSnapshot, snapshot);
                    break;
                }
                case MessageTag.WarMove:
                {
                    var request = reader.MemRead<WarMoveRequest>();
                    WarMoveResult result;
                    try
                    {
                        result = new WarMoveResult { Success = true, Route = WarManager.Move(CharId, request.TargetNodeId) };
                    }
                    catch (InvalidException e)
                    {
                        result = new WarMoveResult { Success = false, Message = e.Message };
                    }

                    MemSend(MessageTag.WarMoveResult, result);
                    break;
                }
                case MessageTag.WarDeployPuppet:
                {
                    var request = reader.MemRead<WarDeployPuppetRequest>();
                    WarMoveResult result;
                    try
                    {
                        var (clan, _) = await GetClan();
                        result = new WarMoveResult
                        {
                            Success = true,
                            Route = WarManager.DeployPuppet(CharId, clan, request.PuppetId, request.TargetNodeId)
                        };
                    }
                    catch (InvalidException e)
                    {
                        result = new WarMoveResult { Success = false, Message = e.Message };
                    }

                    MemSend(MessageTag.WarDeployPuppetResult, result);
                    break;
                }
                case MessageTag.WarCreatePuppet:
                {
                    var request = reader.MemRead<WarCreatePuppetRequest>();
                    WarPuppetResult result;
                    try
                    {
                        var (clan, _) = await GetClan();
                        var charData = client.CharacterData ?? throw new InvalidException("Nhân vật không tồn tại");
                        result = await WarManager.CreatePuppet(charData, clan, request);
                    }
                    catch (InvalidException e)
                    {
                        result = new WarPuppetResult { Success = false, Message = e.Message };
                    }

                    MemSend(MessageTag.WarCreatePuppetResult, result);
                    break;
                }
                case MessageTag.WarUpgradePuppet:
                {
                    var request = reader.MemRead<WarUpgradePuppetRequest>();
                    WarPuppetResult result;
                    try
                    {
                        var (clan, _) = await GetClan();
                        var charData = client.CharacterData ?? throw new InvalidException("Nhân vật không tồn tại");
                        result = await WarManager.UpgradePuppet(charData, clan, request);
                    }
                    catch (InvalidException e)
                    {
                        result = new WarPuppetResult
                            { Success = false, Message = e.Message, IsUpgrade = true, PuppetId = request.PuppetId };
                    }

                    MemSend(MessageTag.WarUpgradePuppetResult, result);
                    break;
                }
                case MessageTag.WarUseHpPill:
                {
                    var request = reader.MemRead<WarUseHpPillRequest>();
                    WarUseHpPillResult result;
                    try
                    {
                        MTS.Models.Clan? clan = null;
                        if (request.PuppetId > 0) (clan, _) = await GetClan();
                        result = WarManager.UseHpPill(client, clan, request);
                        // Lưu tồn kho ngay sau khi trừ đan để không lệch với HP chiến đã cộng.
                        _ = Task.Run(client.SaveChanges);
                    }
                    catch (InvalidException e)
                    {
                        result = new WarUseHpPillResult { Success = false, Message = e.Message };
                    }

                    MemSend(MessageTag.WarUseHpPillResult, result);
                    break;
                }
            }
        }
        catch (Exception e)
        {
            Log.Error(e, "TongMonChien handle {Tag} failed; CharId={CharId}", tag, CharId);
        }
    }

    public void OnLeave()
    {
        left = true;
        Unbind();
        WarManager.Leave(this);
    }

    public void OnDisconnect(IProtocol<MessageTag> protocol)
    {
        // Mất kết nối: không còn được tính là đối thủ; đang đi thì dừng ở node an toàn.
        // Nếu kết nối lại trong thời gian chờ, OnReconnected sẽ vào lại chiến và gửi snapshot mới.
        WarManager.Leave(this);
    }

    public void MemSend<T>(MessageTag tag, T data)
    {
        client.MemSend(tag, data);
    }

    public void Kick(string message)
    {
        left = true;
        Unbind();
        _ = Task.Run(() =>
        {
            MemSend(MessageTag.WarKicked, new WarKicked { Message = message });
            lock (client.StateLock)
            {
                if (ReferenceEquals(client.ClientState, this)) client.ChangeState(new AllMaps(client));
            }
        });
    }

    public void StartCombat(WarBattleMatched matched)
    {
        // Được gọi khi WarManager đang giữ khoá; đổi state ở luồng khác để giữ thứ tự khoá Sync → StateLock.
        _ = Task.Run(() =>
        {
            lock (client.StateLock)
            {
                if (!ReferenceEquals(client.ClientState, this))
                {
                    // Người chơi đã rời scene: trận sẽ hết hạn Ready và bên này bị xử thua.
                    return;
                }

                client.ChangeState(new TongMonChienCombat(client, matched.BattleId));
            }

            MemSend(MessageTag.WarBattleMatched, matched);
        });
    }

    public MessageState State => MessageState.TongMonChien;
}
