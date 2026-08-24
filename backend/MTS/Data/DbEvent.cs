using MTS.Models;

namespace MTS.Data;

public static class DbEvent
{
    public static event Action<UserData> OnUserDeleted;

    public static void InvokeUserDeleted(UserData userData)
    {
        OnUserDeleted?.Invoke(userData);
    }
}