using MTS.Game.TMC;
using MTS.Network.Models;
using Serilog;

namespace MTS.Network.ClientState;

/// <summary>
/// State của một bên khi đang ở màn hình combat Tông môn chiến (PvP thời gian thực hoặc PvE với rối).
/// Chuyển tiếp các message Sync* cho đối thủ PvP; kết quả do <see cref="WarManager"/> xác thực.
/// </summary>
public class TongMonChienCombat : IClientState, IWarCombatSide
{
    private readonly Client client;
    private readonly long battleId;

    public TongMonChienCombat(Client client, long battleId)
    {
        this.client = client;
        this.battleId = battleId;
    }

    public int CharId => client.CharacterData?.Id ?? 0;

    public void OnEnter()
    {
        client.OnLogout += OnClientGone;
        client.OnOtherClientLogin += OnClientGone;
        client.OnDisconnectedSessionExpired += OnClientGone;
    }

    private void Unbind()
    {
        client.OnLogout -= OnClientGone;
        client.OnOtherClientLogin -= OnClientGone;
        client.OnDisconnectedSessionExpired -= OnClientGone;
    }

    private void OnClientGone(Client _)
    {
        Unbind();
        WarManager.OnCombatSideLeft(battleId, CharId);
    }

    public void Handle(Message<MessageTag> message)
    {
        var tag = message.Tag;
        var reader = message.Reader;
        try
        {
            switch (tag)
            {
                case MessageTag.WarReadyCombat:
                    if (!WarManager.OnCombatReady(battleId, this))
                    {
                        MemSend(MessageTag.Notify, "Trận đấu không còn hợp lệ");
                    }

                    break;
                case MessageTag.WarBattleReport:
                    var report = reader.MemRead<WarBattleReport>();
                    if (report.BattleId != battleId) return;
                    WarManager.Report(battleId, this, report);
                    break;
                case MessageTag.SyncAdjustLinhKhi:
                    Relay(tag, reader.MemRead<SyncAdjustLinhKhi>());
                    break;
                case MessageTag.SyncBuff:
                    Relay(tag, reader.MemRead<SyncBuff>());
                    break;
                case MessageTag.SyncDame:
                    Relay(tag, reader.MemRead<SyncDame>());
                    break;
                case MessageTag.SyncEndTurn:
                    Relay(tag, reader.MemRead<SyncEndTurn>());
                    break;
                case MessageTag.SyncHealing:
                    Relay(tag, reader.MemRead<SyncHealing>());
                    break;
                case MessageTag.SyncPlaySkill:
                    Relay(tag, reader.MemRead<SyncPlaySkill>());
                    break;
                case MessageTag.SyncUsePhapBao:
                    Relay(tag, reader.MemRead<SyncUsePhapBao>());
                    break;
                case MessageTag.SyncUseEffectSkill:
                case MessageTag.SyncRemoveEffectSkillNoDestroy:
                    Relay(tag, reader.MemRead<SyncEffectSkill>());
                    break;
                case MessageTag.SyncCPDynamic:
                    Relay(tag, reader.MemRead<SyncCPDynamic>());
                    break;
            }
        }
        catch (Exception e)
        {
            Log.Error(e, "TongMonChienCombat handle {Tag} failed; CharId={CharId} BattleId={BattleId}", tag, CharId, battleId);
        }
    }

    private void Relay<T>(MessageTag tag, T model)
    {
        WarManager.GetPeer(battleId, CharId)?.MemSend(tag, model);
    }

    public void OnLeave()
    {
        Unbind();
        // Trận đã kết thúc thì là no-op; còn đang đánh mà rời màn hình thì xử thua (bỏ chạy).
        WarManager.OnCombatSideLeft(battleId, CharId);
    }

    public void OnDisconnect(IProtocol<MessageTag> protocol)
    {
        WarManager.OnCombatSideLeft(battleId, CharId);
    }

    public void MemSend<T>(MessageTag tag, T data)
    {
        client.MemSend(tag, data);
    }

    public MessageState State => MessageState.TongMonChienCombat;
}
