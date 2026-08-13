using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using MTS.Models;
using MTS.Network.ClientState;
using MTS.Network.Core;
using MTS.Network.Models;
using MTS.Services;
using Network;
using Network.Extensions;
using Serilog;

namespace MTS.Network;

public class Client : ClientConnection, IRequester<MessageTag>
{
    public int Id { get; private set; }
    public string Key { get; private set; }
    private MessageStateHandler<MessageTag, MessageState> stateModule;
    public CoreMessageHandler<MessageTag> CoreModule { get; private set; }
    public UserData? UserData { get; private set; }
    public CharacterData? CharacterData { get; private set; }
    public MailManager MailManager { get; private set; }
    public IMessageState<MessageTag, MessageState> ClientState => stateModule.State;
    public object StateLock { get; } = new();
    public object CharacterLock { get; } = new();
    public object MailLock { get; } = new();
    private object SaveChangesLock { get; } = new();

    public Client(UnauthenticatedClient client)
    {
        Id = client.Id;
        Key = client.Key;
        var tcpClient = client.TcpClient;
        client.TransferProtocol(tcpClient, this);
        TcpClient = tcpClient;
        CoreModule = new CoreMessageHandler<MessageTag>();
        var sessionHandler = new SessionHandler(this);
        CoreModule.RemoveHandler<SessionHandler>();
        CoreModule.AddHandler(sessionHandler);
        stateModule = new MessageStateHandler();
        AddModule(CoreModule);
        AddModule(stateModule);
        Task.Run(Save);
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

    public void SetUserData(UserData? userData)
    {
        UserData = userData;
    }

    protected override void OnError(IProtocol<MessageTag> protocol, Exception ex)
    {
        Log.Error(ex,
            "Client protocol error; ClientId={ClientId}, UserId={UserId}, CharacterId={CharacterId}, CharacterName={CharacterName}",
            Id, UserData?.Id, CharacterData?.Id, CharacterData?.Name);
    }

    protected override async void OnDisconnected(IProtocol<MessageTag> protocol)
    {
        try
        {
            await SaveChanges();
            TcpServer.Instance.Remove(this);
        }
        catch (Exception e)
        {
            Log.Error(e,
                "Failed to handle client disconnect; ClientId={ClientId}, UserId={UserId}, CharacterId={CharacterId}",
                Id, UserData?.Id, CharacterData?.Id);
        }
    }

    public async Task ForceLogout()
    {
        this.MemSend(MessageTag.InGameLogoutResult, new LogoutResult()
        {
            Success = true,
        });
        await SaveChanges();
        SetUserData(null);
        CharacterData = null;
        ChangeState(new AuthState(this));
    }

    public void Send(Span<byte> data)
    {
        TcpClient.Send(data);
    }

    public async Task<CharacterData> Select(CharacterData characterData)
    {
        characterData.Client = this;
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
            .AsSplitQuery()
            .FirstAsync(x => x.CharId == id);
        social.CharacterData = characterData;
        characterData.Social = social;

        var questManager = await db.QuestManagers
            .AsNoTracking()
            .FirstAsync(x => x.CharId == id);
        questManager.CharData = characterData;
        characterData.QuestManager = questManager;
        await questManager.Load(db);

        var arenaManager = await db.ArenaManagers
            .AsNoTracking()
            .Include(x => x.Rewards)
            .AsSplitQuery()
            .FirstAsync(x => x.CharId == id);
        arenaManager.CharData = characterData;
        characterData.ArenaManager = arenaManager;

        var shopManager = await db.ShopManagers
            .AsNoTracking()
            .FirstAsync(x => x.CharId == id);
        shopManager.CharData = characterData;
        characterData.ShopManager = shopManager;

        var gachaManager = await db.GachaManagers
            .AsNoTracking()
            .FirstAsync(x => x.CharId == id);
        gachaManager.CharData = characterData;
        characterData.GachaManager = gachaManager;

        lock (CharacterLock)
        {
            CharacterData = characterData;
            MailManager = new MailManager(characterData, this);
        }

        try
        {
            await WelcomeRewardService.ClaimWelcomeReward(CharacterData, db);
        }
        catch (Exception ex)
        {
            Log.Error(
                ex,
                "Failed to send welcome reward; CharacterId={CharacterId}, CharacterName={CharacterName}",
                characterData.Id,
                characterData.Name);
        }

        Log.Information("Player online: {Name}", CharacterData.Name);
        return characterData;
    }

    public async Task<InventoryManager> GetInventory()
    {
        var shouldLoad = false;
        int id;
        lock (CharacterLock)
        {
            shouldLoad = CharacterData.IsInventoryDefault();
            id = CharacterData.Id;
        }

        if (shouldLoad)
        {
            await using var db = await Program.CreateDbAsync();
            var inventoryManager = await db.InventoryManagers
                .AsNoTracking()
                .FirstAsync(x => x.CharId == id);
            lock (CharacterLock)
            {
                inventoryManager.CharacterData = CharacterData;
            }

            await inventoryManager.Load(db);
            lock (CharacterLock)
            {
                if (CharacterData.IsInventoryDefault())
                {
                    CharacterData.InventoryManager = inventoryManager;
                }
            }
        }

        lock (CharacterLock)
        {
            return CharacterData.InventoryManager;
        }
    }

    public Task SaveChanges()
    {
        lock (SaveChangesLock)
        {
            if (!IsLoggedIn())
            {
                return Task.CompletedTask;
            }

            using var db = Program.CreateDbAsync().GetAwaiter().GetResult();
            try
            {
                UserData!.SaveChanges(db).GetAwaiter().GetResult();
                if (!IsCharacterSelected()) return Task.CompletedTask;
                lock (CharacterLock)
                {
                    CharacterData?.SaveChanges(db).GetAwaiter().GetResult();
                }

                try
                {
                    db.SaveChangesAsync().GetAwaiter().GetResult();
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    Log.Error(
                        ex,
                        "DbUpdateConcurrencyException: {EntryCount} conflicted entries",
                        ex.Entries.Count
                    );
                    foreach (var entry in ex.Entries)
                    {
                        var entityType = entry.Metadata;
                        var primaryKey = entityType.FindPrimaryKey();

                        var keyValues = primaryKey?.Properties
                            .Select(property =>
                            {
                                var propertyEntry = entry.Property(property.Name);

                                return $"{property.Name}=" +
                                       $"{{Current:{FormatValue(propertyEntry.CurrentValue)}, " +
                                       $"Original:{FormatValue(propertyEntry.OriginalValue)}}}";
                            })
                            .ToArray() ?? [];

                        Log.Error(
                            """
                            Concurrency conflict:
                            Entity={Entity}
                            Table={Table}
                            Schema={Schema}
                            State={State}
                            Keys=[{Keys}]
                            """,
                            entityType.ClrType.Name,
                            entityType.GetTableName(),
                            entityType.GetSchema(),
                            entry.State,
                            string.Join(", ", keyValues)
                        );
                        PropertyValues? databaseValues;
                        try
                        {
                            databaseValues = entry.GetDatabaseValuesAsync().GetAwaiter().GetResult();
                        }
                        catch (Exception databaseReadException)
                        {
                            Log.Error(
                                databaseReadException,
                                "Failed to read database values for entity {Entity}, Keys=[{Keys}]",
                                entityType.ClrType.Name,
                                string.Join(", ", keyValues)
                            );

                            continue;
                        }

                        if (databaseValues == null)
                        {
                            Log.Error(
                                """
                                Concurrency reason: ROW_NOT_FOUND.
                                Entity={Entity}
                                Keys=[{Keys}]
                                The row was deleted, or the primary key no longer exists in the database.
                                """,
                                entityType.ClrType.Name,
                                string.Join(", ", keyValues)
                            );

                            foreach (var property in entry.Properties)
                            {
                                Log.Error(
                                    """
                                    Property={Property}
                                    Type={Type}
                                    Original={Original}
                                    Current={Current}
                                    Database=<ROW_NOT_FOUND>
                                    Modified={Modified}
                                    PrimaryKey={PrimaryKey}
                                    ConcurrencyToken={ConcurrencyToken}
                                    StoreGenerated={StoreGenerated}
                                    """,
                                    property.Metadata.Name,
                                    property.Metadata.ClrType.Name,
                                    FormatValue(property.OriginalValue),
                                    FormatValue(property.CurrentValue),
                                    property.IsModified,
                                    property.Metadata.IsPrimaryKey(),
                                    property.Metadata.IsConcurrencyToken,
                                    property.Metadata.ValueGenerated
                                );
                            }

                            continue;
                        }

                        var currentMatchesDatabase = true;
                        var modifiedValuesMatchDatabase = true;

                        foreach (var property in entry.Properties)
                        {
                            var propertyMetadata = property.Metadata;

                            var originalValue = property.OriginalValue;
                            var currentValue = property.CurrentValue;
                            var databaseValue = databaseValues[propertyMetadata];

                            var originalEqualsDatabase = Equals(originalValue, databaseValue);
                            var currentEqualsDatabase = Equals(currentValue, databaseValue);
                            var originalEqualsCurrent = Equals(originalValue, currentValue);

                            if (!currentEqualsDatabase)
                                currentMatchesDatabase = false;

                            if (property.IsModified && !currentEqualsDatabase)
                                modifiedValuesMatchDatabase = false;

                            Log.Error(
                                """
                                Property={Property}
                                Type={Type}
                                Original={Original}
                                Current={Current}
                                Database={Database}
                                Modified={Modified}
                                OriginalEqualsCurrent={OriginalEqualsCurrent}
                                OriginalEqualsDatabase={OriginalEqualsDatabase}
                                CurrentEqualsDatabase={CurrentEqualsDatabase}
                                PrimaryKey={PrimaryKey}
                                ForeignKey={ForeignKey}
                                ConcurrencyToken={ConcurrencyToken}
                                Nullable={Nullable}
                                StoreGenerated={StoreGenerated}
                                """,
                                propertyMetadata.Name,
                                propertyMetadata.ClrType.Name,
                                FormatValue(originalValue),
                                FormatValue(currentValue),
                                FormatValue(databaseValue),
                                property.IsModified,
                                originalEqualsCurrent,
                                originalEqualsDatabase,
                                currentEqualsDatabase,
                                propertyMetadata.IsPrimaryKey(),
                                propertyMetadata.IsForeignKey(),
                                propertyMetadata.IsConcurrencyToken,
                                propertyMetadata.IsNullable,
                                propertyMetadata.ValueGenerated
                            );
                        }

                        if (modifiedValuesMatchDatabase)
                        {
                            Log.Warning(
                                """
                                Concurrency classification: DATABASE_ALREADY_HAS_REQUESTED_VALUES.
                                Entity={Entity}
                                Keys=[{Keys}]
                                Every modified property already has the requested value in the database.
                                This conflict may be safely treated as idempotent success if that matches business rules.
                                """,
                                entityType.ClrType.Name,
                                string.Join(", ", keyValues)
                            );
                        }
                        else if (currentMatchesDatabase)
                        {
                            Log.Warning(
                                """
                                Concurrency classification: CURRENT_ENTITY_MATCHES_DATABASE.
                                Entity={Entity}
                                Keys=[{Keys}]
                                The complete current entity matches the current database row.
                                """,
                                entityType.ClrType.Name,
                                string.Join(", ", keyValues)
                            );
                        }
                        else
                        {
                            Log.Error(
                                """
                                Concurrency classification: DATABASE_VALUES_DIFFER.
                                Entity={Entity}
                                Keys=[{Keys}]
                                One or more requested values differ from the current database values.
                                Do not silently ignore this conflict.
                                """,
                                entityType.ClrType.Name,
                                string.Join(", ", keyValues)
                            );
                        }
                    }

                    throw;
                }
                lock (CharacterLock)
                {
                    CharacterData?.AcceptChanges();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex,
                    "Failed to save client data; ClientId={ClientId}, UserId={UserId}, CharacterId={CharacterId}, Name={Name}",
                    Id, UserData?.Id, CharacterData?.Id, CharacterData?.Name);
            }
        }

        return Task.CompletedTask;
    }

    private static string FormatValue(object? value)
    {
        return value switch
        {
            null => "<null>",
            string text => $"\"{text}\"",
            byte[] bytes => Convert.ToHexString(bytes),
            DateTime dateTime => dateTime.ToString("O"),
            DateTimeOffset dateTimeOffset => dateTimeOffset.ToString("O"),
            _ => value.ToString() ?? "<null>"
        };
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

        if (UserData.Role == Role.Admin)
        {
            module.RemoveHandler<AdminHandler>();
            var adminHandler = new AdminHandler(this);
            module.AddHandler(adminHandler);
        }

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

        var shopHandler = new ShopHandler(this);
        module.RemoveHandler<ShopHandler>();
        module.AddHandler(shopHandler);

        var adsHandler = new AdsHandler(this);
        module.RemoveHandler<AdsHandler>();
        module.AddHandler(adsHandler);

        var rankHandler = new RankHandler(this);
        module.RemoveHandler<RankHandler>();
        module.AddHandler(rankHandler);

        var progressionHandler = new ProgressionHandler(this);
        module.RemoveHandler<ProgressionHandler>();
        module.AddHandler(progressionHandler);

        var inGameAuthHandler = new InGameAuthHandler(this);
        module.RemoveHandler<InGameAuthHandler>();
        module.AddHandler(inGameAuthHandler);

        var syncHandler = new SyncHandler(this);
        module.RemoveHandler<SyncHandler>();
        module.AddHandler(syncHandler);

        var auctionHandler = new AuctionHandler(this);
        module.RemoveHandler<AuctionHandler>();
        module.AddHandler(auctionHandler);
        
        ChangeState(new AllMaps(this));
    }
    public T GetHandler<T>() where T : IClientHandler
    {
        return CoreModule.GetHandler<T>();
    }

    public void AddRequest(MessageTag tag, Action<IReader> response, int timeoutMilliseconds = 0,
        Action<Exception> onError = null)
    {
        AddRequest(TcpClient, tag, response, timeoutMilliseconds, onError);
    }
}
