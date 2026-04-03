using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using MTS.Models;
using MTS.Network.ClientState;
using Network;

namespace MTS.Network;

public class Client : ClientConnection, ISender<MessageTag>
{
    public int Id { get; private set; }
    public string Key { get; private set; }
    private MessageStateHandler<MessageTag, MessageState> stateModule;
    public UserData UserData { get; private set; }
    public CharacterData CharacterData { get; private set; }
    private bool isGetMail;

    public Client(UnauthenticatedClient client)
    {
        Id = client.Id;
        Key = client.Key;
        var tcpClient = client.TcpClient;
        client.TransferProtocol(tcpClient, this);
        TcpClient = tcpClient;
        var coreModule = new CoreMessageHandler<MessageTag>();
        stateModule = new MessageStateHandler<MessageTag, MessageState>();
        AddModule(coreModule);
        AddModule(stateModule);
        _ = Save();
    }

    private async Task Save()
    {
        var interval = TimeSpan.FromSeconds(10);

        while (TcpClient.IsConnected)
        {
            var start = DateTime.UtcNow;
            await SaveChanges();
            var elapsed = DateTime.UtcNow - start;
            var delay = interval - elapsed;
            if (delay > TimeSpan.Zero)
            {
                await Task.Delay(delay);
            }
        }
    }

    public void ChangeState(IClientState state)
    {
        stateModule.ChangeState(state);
    }

    public bool IsLoggedIn()
    {
        return UserData != null;
    }

    public bool IsCharacterSelected()
    {
        return CharacterData != null;
    }

    public void SetUserData(UserData userData)
    {
        UserData = userData;
    }

    protected override void OnError(IProtocol<MessageTag> protocol, Exception ex)
    {
        Console.WriteLine(ex);
    }

    protected override void OnDisconnected(IProtocol<MessageTag> protocol)
    {
        Console.WriteLine($"Client {Id} has been disconnected.");
        _ = SaveChanges();
        TcpServer.Instance.Remove(this);
    }

    public void Send(Span<byte> data)
    {
        TcpClient.Send(data);
    }

    public async Task<CharacterData> Select(CharacterData characterData)
    {
        await using var db = await Program.CreateDbAsync();
        var id = characterData.Id;
        var stats = await db.CharacterStats
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.CharId == id);
        characterData.Stats = stats;
        CharacterData = characterData;
        return characterData;
    }

    public async Task<List<Mail>> GetMails()
    {
        if (!isGetMail)
        {
            await using var db = await Program.CreateDbAsync();
            var id = CharacterData.Id;
            var mails = await db.Mails.AsNoTracking()
                .Where(x => x.CharId == id)
                .ToListAsync();
            CharacterData.Mails = mails;
            isGetMail = true;
        }

        return CharacterData.Mails;
    }

    public async Task<Mail> SendMail(Mail mail)
    {
        mail.CharId = CharacterData.Id;
        await using var db = await Program.CreateDbAsync();
        var entity = await db.Mails.AddAsync(mail);
        await db.SaveChangesAsync();
        return entity.Entity;
    }

    public async Task<InventoryManager> GetInventory()
    {
        if (CharacterData.IsInventoryDefault())
        {
            await using var db = await Program.CreateDbAsync();
            var id = CharacterData.Id;
            var inventoryManager = await db.InventoryManagers.AsNoTracking()
                .FirstAsync(x => x.CharId == id);
            CharacterData.InventoryManager = inventoryManager;
        }

        return CharacterData.InventoryManager;
    }

    public int ReadMail(int id)
    {
        try
        {
            var mail = CharacterData.Mails.FirstOrDefault(x => x.Id == id);
            if (mail == null) return -1;
            if (mail.IsRead) return -1;
            mail.IsRead = true;
            _ = Task.Run(async () =>
            {
                await using var db = await Program.CreateDbAsync();
                await db.Mails
                    .Where(x => x.Id == id)
                    .ExecuteUpdateAsync(s =>
                        s.SetProperty(u => u.IsRead, true));
            });
            return id;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return -1;
        }
    }

    public async Task SaveChanges()
    {
        if (!IsLoggedIn())
        {
            return;
        }

        await using var db = await Program.CreateDbAsync();
        try
        {
            await UserData.SaveChanges(db);
            if (!IsCharacterSelected())
            {
                return;
            }

            await CharacterData.SaveChanges(db);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
        finally
        {
            await db.SaveChangesAsync();
        }
    }

    public int RemoveMail(int id)
    {
        var mails = CharacterData.Mails;
        var mail = mails.FirstOrDefault(x => x.Id == id);
        if (mail == null) return -1;
        _ = Task.Run(async () =>
        {
            await using var db = await Program.CreateDbAsync();
            await db.Mails
                .Where(x => x.Id == id)
                .ExecuteDeleteAsync();
        });
        mails.Remove(mail);
        return id;
    }

    public List<int> RemoveAllMails()
    {
        var mails = CharacterData.Mails;
        var removes = mails.Where(x => x.IsRead && (x.Rewards.Count == 0 || x.IsClaim)).ToList();
        var removeIds = removes.Select(x => x.Id).ToList();
        if (removes.Any())
        {
            mails.RemoveAll(x => removeIds.Contains(x.Id));
            _ = Task.Run(async () =>
            {
                await using var db = await Program.CreateDbAsync();
                await db.Mails
                    .Where(x => removeIds.Contains(x.Id))
                    .ExecuteDeleteAsync();
            });
        }
        return removeIds;
    }
}