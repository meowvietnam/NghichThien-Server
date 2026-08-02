using Microsoft.AspNetCore.Identity.Data;
using MTS.Exceptions;
using Microsoft.EntityFrameworkCore;
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
        private async Task LogoutClientOnline(int userId)
        {
            var server = TcpServer.Instance;
            var clientOnline = server.GetClientOnline(userId);
            if (clientOnline != null)
            {
                await clientOnline.ForceLogout();
                return;
            }
            var disconnectedClient = server.GetDisconnectedClientHasLogin(userId);
            if (disconnectedClient != null)
            {
                await disconnectedClient.ForceLogout();
                server.RemoveDisconnectClient(disconnectedClient);
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
                    lock (client.StateLock)
                    {
                        result = login.Login(client).GetAwaiter().GetResult();
                        var user = result.UserData.User;
                        LogoutClientOnline(user.Id).GetAwaiter().GetResult();
                        client.SetUserData(user);
                        _ = user.UpdateLogin();
                    }
                }
                catch (InvalidException e)
                {
                    result = new LoginResult
                    {
                        Success = false,
                        Message = e.Message
                    };
                }
                finally
                {
                    result.Provider = login.GetSignInProvider();
                    client.MemSend(MessageTag.LoginRequest, result);
                }
            }
            else if (tag == MessageTag.LogoutRequest)
            {
                LogoutResult result = null;
                try
                {
                    lock (client.StateLock)
                    {
                        if (!client.IsLoggedIn())
                        {
                            throw new InvalidException(Strings.NotLoggedIn);
                        }
                        client.SetUserData(null);
                        result = new LogoutResult()
                        {
                            Success = true,
                        };
                    }
                }
                catch (InvalidException e)
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
                    lock (client.StateLock)
                    {
                        if (!client.IsLoggedIn())
                        {
                            throw new InvalidException(Strings.NotLoggedIn);
                        }
                        result = new PlayResult()
                        {
                            Success = true,
                        };
                        client.ChangeState(new SelectCharacter(client));
                    }
                }
                catch (InvalidException e)
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
            else if (tag == MessageTag.DeleteAccountRequest)
            {
                DeleteAccountResult result = null;
                try
                {
                    lock (client.StateLock)
                    {
                        if (!client.IsLoggedIn())
                        {
                            throw new InvalidException(Strings.NotLoggedIn);
                        }

                        result = DeleteAccount(client.UserData.Id).GetAwaiter().GetResult();
                    }
                }
                catch (InvalidException e)
                {
                    result = new DeleteAccountResult
                    {
                        Success = false,
                        Message = e.Message
                    };
                }
                finally
                {
                    client.MemSend(MessageTag.DeleteAccountRequest, result);
                }
            }
            else if (tag == MessageTag.RegisterRequest)
            {
                RegisterResult result = null;
                var request = reader.MemRead<RegisterModel>();
                try
                {
                    lock (client.StateLock)
                    {
                        if (client.IsLoggedIn())
                        {
                            throw new InvalidException(Strings.ClientOnline);
                        }
                        result = RegisterService.Register(request).GetAwaiter().GetResult();
                        client.SetUserData(result.UserData.User);
                    }
                }
                catch (InvalidException e)
                {
                    result = new RegisterResult
                    {
                        Success = false,
                        Message = e.Message
                    };
                }
                finally
                {
                    client.MemSend(MessageTag.RegisterRequest, result);
                }
            }
            else if (tag == MessageTag.GuestRegisterRequest)
            {
                RegisterResult result = null;
                var request = reader.MemRead<GuestRegister>();
                try
                {
                    lock (client.StateLock)
                    {
                        if (!client.IsLoggedIn())
                        {
                            throw new InvalidException(Strings.NotLoggedIn);
                        }
                        result = RegisterService.GuestRegister(request,client.UserData).GetAwaiter().GetResult();
                        client.SetUserData(result.UserData.User);
                    }
                }
                catch (InvalidException e)
                {
                    result = new RegisterResult
                    {
                        Success = false,
                        Message = e.Message
                    };
                }
                finally
                {
                    client.MemSend(MessageTag.GuestRegisterRequest, result);
                }
            }
        }
        private async Task<DeleteAccountResult> DeleteAccount(int userId)
        {
            await using var db = await Program.CreateDbAsync();
            var result = await db.Users
                .Where(x => x.Id == userId)
                .ExecuteDeleteAsync();
            if (result <= 0)
            {
                throw new InvalidException(Strings.DeleteAccountFailed);
            }

            return new DeleteAccountResult()
            {
                Success = true,
            };
        }
        public void OnDisconnect(IProtocol<MessageTag> protocol)
        {
            
        }
        public void OnLeave()
        {
        }
        public MessageState State => MessageState.AuthState;
    }
}
