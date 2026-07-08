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
        private void ValidateOnline(int userId)
        {
            var isClentOnline = TcpServer.Instance.IsClientOnline(userId);
            if (isClentOnline)
            {
                throw new Exception(Strings.ClientOnline);
            }
        }
        public void Handle(Message<MessageTag> message)
        {
            var tag = message.Tag;
            var reader = message.Reader;
            if (tag == MessageTag.LoginRequest)
            {
                var login = reader.MemRead<ILogin>();
                LoginResult result = null;
                try
                {
                    lock (client)
                    {
                        result = login.Login().GetAwaiter().GetResult();
                        var user = result.UserData.User;
                        ValidateOnline(user.Id);
                        client.SetUserData(user);
                    }
                }
                catch (Exception e)
                {
                    result = new LoginResult
                    {
                        Success = false,
                        Message = e.Message
                    };
                }
                finally
                {
                    client.MemSend(MessageTag.LoginRequest, result);
                }
            }
            else if (tag == MessageTag.LogoutRequest)
            {
                LogoutResult result = null;
                try
                {
                    lock (client)
                    {
                        if (!client.IsLoggedIn())
                        {
                            throw new Exception(Strings.NotLoggedIn);
                        }
                        client.SetUserData(null);
                        result = new LogoutResult()
                        {
                            Success = true,
                        };
                    }
                }
                catch (Exception e)
                {
                    result = new LogoutResult
                    {
                        Success = false,
                        Message = e.Message
                    };
                }
                finally
                {
                    client.MemSend(MessageTag.LogoutRequest, result);
                }
            }
            else if (tag == MessageTag.PlayRequest)
            {
                PlayResult result = null;
                try
                {
                    lock (client)
                    {
                        if (!client.IsLoggedIn())
                        {
                            throw new Exception(Strings.NotLoggedIn);
                        }
                        result = new PlayResult()
                        {
                            Success = true,
                        };
                        client.ChangeState(new SelectCharacter(client));
                    }
                }
                catch (Exception e)
                {
                    result = new PlayResult
                    {
                        Success = false,
                        Message = e.Message
                    };
                }
                finally
                {
                    client.MemSend(MessageTag.PlayRequest, result);
                }
            }
        }

        public void OnLeave()
        {
        }

        public MessageState State => MessageState.AuthState;
    }
}
