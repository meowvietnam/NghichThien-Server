using MTS.Network.ClientState;

namespace MTS.Network.Core;

public class SessionHandler : IClientHandler
{
    private readonly Client client;
    private DateTime lastSendPing = DateTime.UtcNow;
    private DateTime lastPong = DateTime.UtcNow;
    private bool isConnected = true;

    public SessionHandler(Client client)
    {
        this.client = client;
    }

    private async Task SendPingLoop()
    {
        var interval = TimeSpan.FromSeconds(15);
        while (isConnected)
        {
            await Task.Delay(interval);
            lastSendPing = DateTime.UtcNow;
            client.Send(MessageTag.Ping);
        }
    }

    private async Task CheckLive()
    {
        var interval = TimeSpan.FromSeconds(30);
        while (isConnected)
        {
            await Task.Delay(interval);
            if (!client.IsLoggedIn())
            {
                continue;
            }
            var adsHandler = client.GetHandler<AdsHandler>();
            if (adsHandler != null)
            {
                if (adsHandler.IsWatching())
                {
                    continue;
                }
            }
            
            var deltaTime = lastSendPing - lastPong;
            if (deltaTime > interval)
            {
                //client.TcpClient.Disconnect();
            }
        }
    }

    public void OnEnter()
    {
        SendPingLoop();
        CheckLive();
    }

    public void Handle(Message<MessageTag> message)
    {
        var tag = message.Tag;
        if (tag == MessageTag.Ping)
        {
            client.Send(MessageTag.Pong);
        }
        else if (tag == MessageTag.Pong)
        {
            lastPong = DateTime.UtcNow;
        }
    }

    public void OnLeave()
    {
    }

    public void OnDisconnect(IProtocol<MessageTag> protocol)
    {
        isConnected = false;
    }

    private static readonly MessageTag[] handlers = { MessageTag.Ping, MessageTag.Pong };
    public MessageTag[] HandleTag => handlers;
}