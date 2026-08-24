using MTS.Models;
using MTS.Network;
using MTS.Network.Models;

namespace MTS.Game.DP;

public class DongPhuObserver
{
    public DongPhu DongPhu { get; set; }
    public HashSet<Client> Clients { get; set; } = new ();
    public DongPhuObserver(DongPhu dongPhu)
    {
        DongPhu = dongPhu;
    }
    public void AddClient(Client client)
    {
        client.OnOtherClientLogin -= OnOtherClientLogin;
        client.OnLogout -= OnLogout;
        client.OnRemoveDisconnectedSession -= OnRemoveDisconnectedSession;
    }
    public void RemoveClient(Client client)
    {
        if (Clients.Remove(client))
        {
            client.OnOtherClientLogin -= OnOtherClientLogin;
            client.OnLogout -= OnLogout;
            client.OnRemoveDisconnectedSession -= OnRemoveDisconnectedSession;
        }
        DongPhuManager.OnRemoveClient(this, client);
    }
    private void OnRemoveDisconnectedSession(Client client)
    {
        RemoveClient(client);
    }
    private void OnLogout(Client client)
    {
        RemoveClient(client);
    }
    private void OnOtherClientLogin(Client client)
    {
        RemoveClient(client);
    }
    public void OnUserDeleted()
    {
    }
    public void OnDongPhuLiked(DongPhuLike like)
    {
    }
    public void OnWarteringHelp(AddWateringHelp addWateringHelp)
    {
    }
}