using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using MTS.Models;
using MTS.Network.ClientState;
using MTS.Network.Core;
using MTS.Network.Models;
using Network;
using Network.Extensions;

namespace MTS.Network;

public class Client : ClientConnection, ISender<MessageTag>, IRequester<MessageTag>
{
    public int Id { get; private set; }
    public string Key { get; private set; }
    private MessageStateHandler<MessageTag, MessageState> stateModule;
    public CoreMessageHandler<MessageTag> CoreModule { get; private set; }
    public UserData UserData { get; private set; }
    public CharacterData CharacterData { get; private set; }
    public MailManager MailManager { get; private set; }
    public IMessageState<MessageTag,MessageState> ClientState => stateModule.State;

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
            .FirstAsync(x => x.CharId == id);
        stats.CharacterData = characterData;
        characterData.Stats = stats;
        var equipment = await db.EquipmentManagers
            .AsNoTracking()
            .Include(x => x.ThanThongEquips)
            .Include(x => x.DanCombatUseds)
            .AsSplitQuery()
            .FirstAsync(x => x.CharId == id);
        equipment.CharacterData = characterData;
        characterData.EquipmentManager = equipment;
        var social = await db.Socials
            .AsNoTracking()
            .Include(x => x.Relationships)
            .FirstAsync(x => x.CharId == id);
        social.CharacterData = characterData;
        characterData.Social = social;
        var questManager = await db.QuestManagers
            .AsNoTracking()
            .FirstAsync(x => x.CharId == id);
        questManager.CharData = characterData;
        characterData.QuestManager = questManager;
        CharacterData = characterData;
        MailManager = new MailManager(characterData, this);
        return characterData;
    }

    public async Task<InventoryManager> GetInventory()
    {
        if (CharacterData.IsInventoryDefault())
        {
            await using var db = await Program.CreateDbAsync();
            var id = CharacterData.Id;
            var inventoryManager = await db.InventoryManagers
                .AsNoTracking()
                .FirstAsync(x => x.CharId == id);
            inventoryManager.CharacterData = CharacterData;
            await inventoryManager.Load(db);
            CharacterData.InventoryManager = inventoryManager;
        }
        return CharacterData.InventoryManager;
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
            if (!IsCharacterSelected()) return;
            lock (CharacterData)
            {
                CharacterData.SaveChanges(db).GetAwaiter().GetResult();
            }
            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    public async Task<RewardResult?> ClaimMail(int id)
    {
        var inventory = await GetInventory();
        return await MailManager.ClaimMail(id, inventory);
    }

    public async Task<RewardResult?> ClaimAllMails()
    {
        var inventory = await GetInventory();
        return await MailManager.ClaimAllMails(inventory);
    }
    
    public void OnSelectedCharacter()
    {
        var module = CoreModule;
        
        module.RemoveHandler<MailHandler>();
        var mailHandler = new MailHandler(this);
        module.AddHandler(mailHandler);
        
        module.RemoveHandler<InventoryHandler>();
        var inventoryHandler = new InventoryHandler(this);
        module.AddHandler(inventoryHandler);
        
        module.RemoveHandler<AdminHandler>();
        var adminHandler = new AdminHandler(this);
        module.AddHandler(adminHandler);
        
        var sceneManager = new SceneManager(this);
        module.RemoveHandler<SceneManager>();
        module.AddHandler(sceneManager);
        
        var skillHandler = new SkillHandler(this);
        module.RemoveHandler<SkillHandler>();
        module.AddHandler(skillHandler);

        var monPhaiHandler = new MonPhaiHandler(this);
        module.RemoveHandler<MonPhaiHandler>();
        module.AddHandler(monPhaiHandler);

        var questHandler = new QuestHandler(this);
        module.RemoveHandler<QuestHandler>();
        module.AddHandler(questHandler);

        var combatHandler = new CombatHandler(this);
        module.RemoveHandler<CombatHandler>();
        module.AddHandler(combatHandler);

        var socialHandler = new SocialHandler(this);
        module.RemoveHandler<SocialHandler>();
        module.AddHandler(socialHandler);

        var iapHandler = new IapHandler(this);
        module.RemoveHandler<IapHandler>();
        module.AddHandler(iapHandler);
        
        ChangeState(new AllMaps(this));
    }

    public void AddRequest(MessageTag tag, Action<IReader> response, int timeoutMilliseconds = 0, Action<Exception> onError = null)
    {
        AddRequest(TcpClient ,tag, response, timeoutMilliseconds, onError);
    }
}