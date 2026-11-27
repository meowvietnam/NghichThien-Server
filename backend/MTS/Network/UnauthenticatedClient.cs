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
            "apk_v1.8.5", "apk_v1.8.4", "apk_v1.8.3", "ios_v1.8.3",
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
                        // Socket đã chuyển sang client cũ, read loop của client tạm dừng mà không gọi OnDisconnected.
                        TcpServer.Instance.Remove(client);
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

    // Trước khi xác thực chỉ có Connect/Reconnect/CheckVersion, đều rất nhỏ.
    public override bool ValidatePacketSize(int size)
    {
        return size >= 0 && size <= 1024;
    }

    protected override void OnError(IProtocol<MessageTag> protocol, Exception ex)
    {
        // Gói bị từ chối vẫn còn phần thân trong stream, đọc tiếp sẽ lệch header -> ngắt luôn.
        if (ex is InvalidPacketSizeException)
        {
            protocol.Disconnect();
        }
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