using MTS.Network.Models;
using Network;
using NativeTcp = System.Net.Sockets.TcpClient;
using Network.Extensions;
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
                var success = true;
                client.TcpClient.Send(MessageTag.CheckVersionResult, success);
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
    private class ConnectState(UnauthenticatedClient client) : IMessageState<MessageTag,MessageState>
    {
        public MessageState State => MessageState.ConnectState;
        private bool isAuthenticated;
        private const string ConnectSuccess = "Kết nối tới máy chủ thành công!";
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
                    Message = ConnectSuccess,
                    Id = client.Id,
                    Key = client.Key
                };
                client.TcpClient.Send(MessageTag.ConnectResult, result);
                client.ChangeState(new CheckVersionState(client));
            }
            else if (tag == MessageTag.Reconnect) 
            {
            }
        }
        public void OnLeave()
        {
        }
    }
    public int Id { get; private set; }
    public string Key { get; private set; }
    private MessageStateHandler<MessageTag,MessageState> module;

    public UnauthenticatedClient(int id, string key, NativeTcp client) 
    {
        Id = id;
        Key = key;
        module = new MessageStateHandler<MessageTag,MessageState>();
        module.ChangeState(new ConnectState(this));
        AddModule(module);
        TcpClient = new TcpClient(client);
        AddProtocol(TcpClient);
        TcpClient.StartRead();
    }
    private void ChangeState(IMessageState<MessageTag,MessageState> state)
    {
        module.ChangeState(state);
    }
    protected override void OnDisconnected(IProtocol<MessageTag> protocol)
    {
        TcpServer.Instance.Remove(this);
    }
}
