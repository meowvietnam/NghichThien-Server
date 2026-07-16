using MemoryPack;

namespace MTS.Network.Models;
[MemoryPackable]
[MemoryPackUnion(0, typeof(LoginWithGoogle))]
[MemoryPackUnion(1, typeof(AdminLoginWithGoogle))]
public partial interface ILogin
{
    SignInProvider GetSignInProvider();
    Task<LoginResult> Login();
}