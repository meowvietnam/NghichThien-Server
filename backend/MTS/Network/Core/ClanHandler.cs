using MTS.Services.Clans;
using Newtonsoft.Json;
using Network.Models.Clans;
using Serilog;

namespace MTS.Network.Core;

public sealed class ClanHandler : IClientHandler
{
    private readonly Client client;
    private readonly ClanService service;
    private readonly ClanRealtimeService realtime;
    private readonly int charId;
    private int busy;
    private long nextAllowedQuery;
    private long nextAllowedCommand;
    private bool left;
    public ClanHandler(Client client)
    {
        this.client = client;
        charId = client.CharacterData!.Id;
        service = Program.App.Services.GetRequiredService<ClanService>();
        realtime = Program.App.Services.GetRequiredService<ClanRealtimeService>();
    }
    public MessageTag[] HandleTag => [MessageTag.ClanCommand, MessageTag.ClanQuery];
    public void OnEnter()
    {
        realtime.Subscribe(charId, Push);
        client.OnLogout += Remove;
        client.OnOtherClientLogin += Remove;
        client.OnDisconnectedSessionExpired += Remove;
        client.OnSessionReconnected += Reconnect;
    }
    private void Remove(Client _) => OnLeave();
    private void Reconnect(Client _) { if (!left) realtime.Subscribe(charId, Push); }
    public void OnDisconnect(IProtocol<MessageTag> protocol) => realtime.Unsubscribe(charId, Push);
    public void OnLeave()
    {
        left = true;
        realtime.Unsubscribe(charId, Push);
        client.OnLogout -= Remove;
        client.OnOtherClientLogin -= Remove;
        client.OnDisconnectedSessionExpired -= Remove;
        client.OnSessionReconnected -= Reconnect;
    }
    private void Push(ClanChangedDto change)
    {
        if (!left && client.TcpClient.IsConnected && client.CharacterData?.Id == charId)
            client.Send(MessageTag.ClanChanged, JsonConvert.SerializeObject(change));
    }
    public void Handle(Message<MessageTag> message)
    {
        var tag = message.Tag;
        ClanCommandRequest? command = null;
        ClanQueryRequest? query = null;
        Guid requestId = Guid.Empty;
        try
        {
            var byteLength = message.Reader.Read<int>();
            if (byteLength < 0 || byteLength > 16384) throw new JsonException("Invalid payload size");
            var json = StringEncode.Encoding.GetString(message.Reader.GetNext(byteLength));
            if (json.Length > 8192) throw new JsonException("Invalid payload size");
            var jsonSettings = new JsonSerializerSettings { MaxDepth = 8, TypeNameHandling = TypeNameHandling.None };
            if (tag == MessageTag.ClanCommand)
            {
                command = JsonConvert.DeserializeObject<ClanCommandRequest>(json, jsonSettings) ?? throw new JsonException();
                requestId = command.RequestId;
            }
            else
            {
                query = JsonConvert.DeserializeObject<ClanQueryRequest>(json, jsonSettings) ?? throw new JsonException();
                requestId = query.RequestId;
            }
        }
        catch (Exception ex) when (ex is JsonException or ArgumentException)
        {
            Reply(tag, new ClanResponse { RequestId = requestId, Code = "InvalidRequest" });
            return;
        }
        if (Interlocked.CompareExchange(ref busy, 1, 0) != 0)
        {
            Reply(tag, new ClanResponse { RequestId = requestId, Code = "Busy" });
            return;
        }
        var now = Environment.TickCount64;
        var next = tag == MessageTag.ClanQuery ? nextAllowedQuery : nextAllowedCommand;
        if (now < next)
        {
            Interlocked.Exchange(ref busy, 0);
            Reply(tag, new ClanResponse { RequestId = requestId, Code = "RateLimited" });
            return;
        }
        if (tag == MessageTag.ClanQuery) nextAllowedQuery = now + 200;
        else nextAllowedCommand = now + 100;
        _ = Task.Run(() =>
        {
            try
            {
                ClanResponse response;
                // Share the existing character lock with autosave and inventory/skill handlers.
                lock (client.CharacterLock)
                {
                    if (left || !client.IsLoggedIn() || client.CharacterData?.Id != charId)
                        response = new ClanResponse { RequestId = requestId, Code = "CharacterRequired" };
                    else response = command != null ? service.Execute(new Actor(client), command) : service.Query(charId, query!);
                }
                Reply(tag, response);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Clan request failed for character {CharId}, request {RequestId}", charId, requestId);
                Reply(tag, new ClanResponse { RequestId = requestId, Code = "ServerError" });
            }
            finally { Interlocked.Exchange(ref busy, 0); }
        });
    }
    private void Reply(MessageTag tag, ClanResponse response) => client.Send(tag, JsonConvert.SerializeObject(response));
    private sealed class Actor(Client client) : IClanActor
    {
        public object SyncRoot => client.CharacterLock;
        public int CharId => client.CharacterData!.Id;
        public int Level => client.CharacterData!.Progression.Level;
        public int MoneyVip
        {
            get => client.CharacterData!.MoneyVip;
            set => client.CharacterData!.SetMoneyVip(value);
        }
    }
}
