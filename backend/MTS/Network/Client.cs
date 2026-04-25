using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using MTS.Models;
using MTS.Network.ClientState;
using MTS.Network.Models;
using Network;
using Network.Extensions;

namespace MTS.Network;

public class Client : ClientConnection, ISender<MessageTag>
{
    public int Id { get; private set; }
    public string Key { get; private set; }
    private MessageStateHandler<MessageTag, MessageState> stateModule;
    public CoreMessageHandler<MessageTag> CoreModule { get; private set; }
    public UserData UserData { get; private set; }
    public CharacterData CharacterData { get; private set; }
    public IMessageState<MessageTag,MessageState> ClientState => stateModule.State;
    private bool isGetMail;

    public Client(UnauthenticatedClient client)
    {
        Id = client.Id;
        Key = client.Key;
        var tcpClient = client.TcpClient;
        client.TransferProtocol(tcpClient, this);
        TcpClient = tcpClient;
        CoreModule = new CoreMessageHandler<MessageTag>();
        stateModule = new MessageStateHandler<MessageTag, MessageState>();
        AddModule(CoreModule);
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
    public void Request(MessageTag tag, Action<IReader> callback, int timeoutMilliseconds = 0)
    {
        AddRequest(TcpClient, tag, callback, timeoutMilliseconds);
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
        var newMail = entity.Entity;
        if (IsLoggedIn())
        {
            this.Send(MessageTag.NewMail, newMail);
        }
        return newMail;
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

    public async Task<RewardResult?> ClaimMail(int id)
    {
        var mails = CharacterData.Mails;
        var mail = mails.FirstOrDefault(x => x.Id == id);
        if (mail == null)
        {
            return null;
        }
        if (mail.IsClaim) return null;
        var rewards = mail.Rewards;
        var inventory = await GetInventory();
        var result = inventory.AddToInventory(rewards);
        mail.IsClaim = true;
        _ = Task.Run(async () =>
        {
            await using var db = await Program.CreateDbAsync();
            await db.Mails
                .Where(x => x.Id == id)
                .ExecuteUpdateAsync(s =>
                    s.SetProperty(u => u.IsClaim, true));
        });
        return new RewardResult()
        {
            Mails = new List<int>() {id},
            RewardItems = result
        };
    }

    public async Task<RewardResult?> ClaimAllMails()
    {
        var mails = CharacterData.Mails;
        var neverClaims = mails.Where(x => !x.IsClaim).ToList();
        if (!neverClaims.Any())
        {
            return null;
        }
        var mailIds = neverClaims.Select(x => x.Id).ToList();
        var rewards = neverClaims.SelectMany(x => x.Rewards).ToList();
        var inventory = await GetInventory();
        var result = inventory.AddToInventory(rewards);
        _ = Task.Run(async () =>
        {
            await using var db = await Program.CreateDbAsync();
            await db.Mails
                .Where(x => mailIds.Contains(x.Id))
                .ExecuteUpdateAsync(s =>
                    s.SetProperty(u => u.IsClaim, true));
        });
        foreach (var neverClaim in neverClaims)
        {
            neverClaim.IsClaim = true;
        }
        return new RewardResult()
        {
            Mails = mailIds,
            RewardItems = result
        };
    }
}