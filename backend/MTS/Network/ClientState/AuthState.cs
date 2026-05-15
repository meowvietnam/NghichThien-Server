using MTS.Localization;
using MTS.Network.Models;
using MTS.Services;
using Network;
using Network.Extensions;

namespace MTS.Network.ClientState
{
    public class AuthState(Client client) : IClientState
    {
        public void OnEnter()
        {
        }
        public async void Handle(Message<MessageTag> message)
        {
            var tag = message.Tag;
            var reader = message.Reader;
            if (tag == MessageTag.LoginDeveloper)
            {
                var username = reader.Read();
                var service = new AuthService();
                try
                {
                    var userData = await service.CreateOrLogin(username);
                    var isClentOnline = TcpServer.Instance.IsClientOnline(userData.Id);
                    if (isClentOnline)
                    {
                        throw new Exception(Strings.ClientOnline);
                    }
                    client.SetUserData(userData);
                    var result = new LoginResult
                    {
                        Success = true,
                        Message = Strings.LoginSuccess,
                        UserData = userData
                    };
                    client.Send(MessageTag.LoginDeveloperResult, result);
                    client.ChangeState(new SelectCharacter(client));
                }
                catch (Exception e)
                {
                    var result = new LoginResult
                    {
                        Success = false,
                        Message = e.Message
                    };
                    client.Send(MessageTag.LoginDeveloperResult, result);
                }
            }
        }
        public void OnLeave()
        {
        }
        public MessageState State => MessageState.AuthState;
    }
}