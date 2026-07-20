using System.Collections.Concurrent;
using System.Net.Sockets;
using Microsoft.VisualBasic.CompilerServices;
using MTS.Game;
using MTS.Localization;
using MTS.Models;
using MTS.Network.ClientState;
using MTS.Network.Core;
using NetworkServer;
using Serilog;

namespace MTS.Network;

public class TcpServer : BaseTcpServer
{
    public static TcpServer Instance { get; private set; }
    private ConcurrentDictionary<int, UnauthenticatedClient> unauthenticatedClients = new();
    private ConcurrentDictionary<int, Client> clients = new();
    private ConcurrentDictionary<int, Client> disconnectedClients = new();
    private int currentId = int.MinValue;
    public TcpServer(int port) : base(port)
    {
        Instance = this;
    }
    protected override void OnAccept(System.Net.Sockets.TcpClient tcpClient)
    {
        var id = Interlocked.Increment(ref currentId);
        var key = Utils.Utils.GenerateSecureString(20);
        var unauth = new UnauthenticatedClient(id, key, tcpClient);
        unauthenticatedClients.TryAdd(id, unauth);
    }

    protected override void OnError(Exception ex)
    {
        if (ex is SocketException socketException)
        {
            Log.Debug(socketException, "TCP socket error ignored");
            return;
        }
        Log.Error(ex, "TCP server error");
    }

    protected override void OnStop()
    {
        
    }
    protected override void OnRun()
    {
        Log.Information("TCP server is running on port {Port}", Port);
    }
    public void Remove(UnauthenticatedClient unauthenticatedClient)
    {
        unauthenticatedClients.TryRemove(unauthenticatedClient.Id, out _);
    }
    public async Task Remove(Client client)
    {
        clients.TryRemove(client.Id, out _);
        disconnectedClients.TryAdd(client.Id, client);
        await Task.Delay(TimeSpan.FromMinutes(5));
        disconnectedClients.TryRemove(client.Id, out _);
    }
    public void AuthenticatedSuccess(UnauthenticatedClient client)
    {
        var newClient = new Client(client);

        clients.TryAdd(client.Id, newClient);
        newClient.ChangeState(new AuthState(newClient));
    }

    public CharacterData[] GetCharsOnline()
    {
        return clients.Values
            .Where(x => x.IsCharacterSelected())
            .Select(x => x.CharacterData)
            .ToArray();
    }

    public async Task SaveClients()
    {
        var semaphore = new SemaphoreSlim(10);
        var tasks = clients.Values.Select(async client =>
        {
            await semaphore.WaitAsync();
            try
            {
                await client.SaveChanges();
            }
            finally
            {
                semaphore.Release();
            }
        });
        await Task.WhenAll(tasks);
        Log.Information("{Message}; saved {ClientCount} online clients", Strings.SaveChangesCompleted, clients.Count);
    }
    public Client? GetClientOnline(int userDataId)
    {
        return clients.Values.FirstOrDefault(x => x.IsLoggedIn() && x.UserData.Id == userDataId);
    }
    public Client? GetDisconnectedClient(Reconnect request)
    {
        var id = request.Id;
        var key = request.Key;
        if (disconnectedClients.TryGetValue(id, out var client))
        {
            if(client.Key == key)
            {
                return client;
            }
        }
        return null;
    }

    public Client? GetDisconnectedClientHasLogin(int userId)
    {
        return disconnectedClients.Values.FirstOrDefault(x => x.IsLoggedIn() && x.UserData.Id == userId);
    }
    public void RemoveDisconnectClient(Client client)
    {
        disconnectedClients.TryRemove(client.Id, out _);
    }

    public void OnReconnected(Client client)
    {
        RemoveDisconnectClient(client);
        clients.TryAdd(client.Id, client);
    }

    public Client[] GetClientsOnline()
    {
        return clients.Values.ToArray();
    }
    public Client[] GetClientsDisconnected()
    {
        return disconnectedClients.Values.ToArray();
    }
}
