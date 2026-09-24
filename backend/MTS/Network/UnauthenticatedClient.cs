using MTS.Exceptions;
using MTS.Localization;
using MTS.Models;
using MTS.Network.Models;
using NativeTcp = System.Net.Sockets.TcpClient;

namespace MTS.Network;

public class UnauthenticatedClient : ClientConnection
{
    private enum MessageState
    {
        ConnectState,
        CheckVersionState
    }

    private class CheckVersionState(UnauthenticatedClient client) : IMessageState<MessageTag, MessageState>
    {
        private static readonly string[] acceptVersions =
        [
            "apk_v1.7.4", "ios_v1.7.4",
        ];

        public void OnEnter()
        {
        }

        public void Handle(Message<MessageTag> message)
        {
            var tag = message.Tag;
            var reader = message.Reader;
            if (tag == MessageTag.CheckVersion)
            {
                var version = reader.Read();
                var success = acceptVersions.Contains(version);
                var msg = success
                    ? "Phiên bản hợp lệ"
                    : "Phiên bản không hợp lệ, hãy tải phiên bản mới!";
                var result = new CheckVersionResult()
                {
                    Success = success,
                    Message = msg
                };
                client.TcpClient.MemSend(MessageTag.CheckVersionResult, result);
                if (success)
                {
                    TcpServer.Instance.AuthenticatedSuccess(client);
                }
            }
        }

        public void OnLeave()
        {
        }

        public MessageState State => MessageState.CheckVersionState;
    }

    private class ConnectState(UnauthenticatedClient client) : IMessageState<MessageTag, MessageState>
    {
        public MessageState State => MessageState.ConnectState;
        private bool isAuthenticated;

        public void OnEnter()
        {
            _ = CheckAuthentication();
        }

        private async Task CheckAuthentication()
        {
            await Task.Delay(5000);
            if (!isAuthenticated)
            {
                client.TcpClient.Disconnect();
            }
        }

        public void Handle(Message<MessageTag> message)
        {
            var tag = message.Tag;
            var reader = message.Reader;
            if (tag == MessageTag.Connect)
            {
                isAuthenticated = true;
                var result = new ConnectResult()
                {
                    Success = true,
                    Message = Strings.ConnectSuccess,
                    Id = client.Id,
                    Key = client.Key
                };
                client.TcpClient.Send(MessageTag.ConnectResult, result);
                client.ChangeState(new CheckVersionState(client));
            }
            else if (tag == MessageTag.Reconnect)
            {
                Result result = null!;
                var tcp = client.TcpClient;
                try
                {
                    lock (client)
                    {
                        var request = reader.MemRead<Reconnect>();
                        var disconnectedClient = TcpServer.Instance.GetDisconnectedClient(request);
                        if (disconnectedClient == null)
                        {
                            throw new InvalidException(Strings.ReconnectClientNotFound);
                        }

                        isAuthenticated = true;
                        disconnectedClient.TcpClient.Reconnect(client.TcpClient);
                        TcpServer.Instance.OnReconnected(disconnectedClient);
                        tcp = disconnectedClient.TcpClient;
                        result = new Result()
                        {
                            Success = true,
                        };
                    }
                }
                catch (InvalidException e)
                {
                    result = new Result()
                    {
                        Success = false,
                        Message = e.Message,
                    };
                }
                finally
                {
                    tcp.MemSend(MessageTag.ReconnectResult, result);
                }
            }
        }

        public void OnLeave()
        {
        }
    }

    public int Id { get; private set; }
    public string Key { get; private set; }
    private MessageStateHandler<MessageTag, MessageState> module;

    public UnauthenticatedClient(int id, string key, NativeTcp client)
    {
        Id = id;
        Key = key;
        module = new MessageStateHandler<MessageTag, MessageState>();
        module.ChangeState(new ConnectState(this));
        AddModule(module);
        TcpClient = new TcpClient(client);
        AddProtocol(TcpClient);
        TcpClient.StartRead();
    }

    private void ChangeState(IMessageState<MessageTag, MessageState> state)
    {
        module.ChangeState(state);
    }

    protected override void OnDisconnected(IProtocol<MessageTag> protocol)
    {
        TcpServer.Instance.Remove(this);
    }
}