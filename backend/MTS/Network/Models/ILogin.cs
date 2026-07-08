using MemoryPack;

namespace MTS.Network.Models;
[MemoryPackable]
[MemoryPackUnion(0, typeof(LoginWithGoogle))]
public partial interface ILogin
{
    SignInProvider GetSignInProvider();
    Task<LoginResult> Login();
}