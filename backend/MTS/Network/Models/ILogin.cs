using MemoryPack;

namespace MTS.Network.Models;
[MemoryPackable]
[MemoryPackUnion(0, typeof(LoginWithGoogle))]
[MemoryPackUnion(1, typeof(AdminLoginWithGoogle))]
[MemoryPackUnion(2, typeof(LoginWithUsernamePassword))]
[MemoryPackUnion(3, typeof(LoginGuest))]
public partial interface ILogin
{
    SignInProvider GetSignInProvider();
    Task<LoginResult> Login(Client client);
}