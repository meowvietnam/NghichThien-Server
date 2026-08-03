using MemoryPack;

namespace MTS.Network.Models;

[MemoryPackable]
[MemoryPackUnion(0, typeof(LoginWithGoogle))]
[MemoryPackUnion(1, typeof(AdminLoginWithGoogle))]
[MemoryPackUnion(2, typeof(LoginWithUsernamePassword))]
[MemoryPackUnion(3, typeof(LoginGuest))]
[MemoryPackUnion(4, typeof(AdminLoginWithName))]
public partial interface ILogin
{
    SignInProvider GetSignInProvider();
    Task<LoginResult> Login(Client client);
    [MemoryPackIgnore] protected const string Key = "qnn22n2us7g2bbna-aql11.e22111m1m";
}