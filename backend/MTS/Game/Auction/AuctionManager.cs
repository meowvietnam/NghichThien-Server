using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using MTS.Data;
using MTS.Exceptions;
using MTS.Game.BaseSkill;
using MTS.Game.LuyenDan;
using MTS.Localization;
using MTS.Models;
using MTS.Network;
using MTS.Network.Models;
using MTS.Utils;
using Serilog;

namespace MTS.Game.Auction;

public class AuctionManager
{
    public static AuctionManager Instance { get; private set; }
    public State ThisState { get; private set; }
    private int playerIdCounter = 0;
    private object idLock = new();
    public PlayerAuctionModel[] PlayerModels { get; private set; }
    private object playerModelsLock = new();
    private PlayerAuctionModel[] poolPlayerModels;
    public ConcurrentDictionary<int, PlayerAuction> Players { get; private set; } = new();
    public AuctionSession[] Sessions { get; private set; }
    public int CurrentSessionIndex { get; private set; }
    public AuctionSession CurrentSession => Sessions[CurrentSessionIndex];
    private object currentSessionLock = new();
    public DateTime StartTime { get; private set; }
    private CancellationTokenSource cts;
    private string content;
    public event Action OnAuctionEnded;

    public AuctionManager(string content)
    {
        Instance = this;
        ThisState = State.NotStarted;
        this.content = content;
        cts = new CancellationTokenSource();
    }

    public AuctionManager(AuctionHistory history, string content)
    {
        Instance = this;
        ThisState = State.Ended;
        this.content = content;
        cts = new CancellationTokenSource();
        StartTime = history.StartTime;
        Sessions = history.Sessions;
    }

    public async Task Init()
    {
        StartTime = DateTime.UtcNow;
        CreateSessions();
        await using var db = await Program.CreateDbAsync();
        var charDatas = await db.Characters
            .AsNoTracking()
            .Include(x => x.Appearance)
            .Include(x => x.Progression)
            .OrderBy(x => x.Progression.Level)
            .Take(10)
            .ToArrayAsync();
        PlayerModels = charDatas.Select(c =>
        {
            var id = GetNewId();
            return new PlayerAuctionModel(c, id);
        }).ToArray();
        poolPlayerModels = PlayerModels.ToArray();
        ThisState = State.Started;
        _ = Run();
    }

    private ItemRef RandomThanThongThienGiai()
    {
        var thanThongs = GameConfig.NDCPTTManager.ThanThongs;
        var tiers = new[] { ETierSkill.ThienGiaiHa, ETierSkill.ThienGiaiTrung, ETierSkill.ThienGiaiThuong };
        var items = thanThongs
            .Where(i => tiers.Contains(i.eTierSkill))
            .ToArray();
        var item = items.Random();
        return new ItemRef()
        {
            Id = (ushort)item.Id,
            Type = ItemType.ThanThong,
            Count = 1,
        };
    }

    private ItemRef RandomThanThongDiaGiai()
    {
        var thanThongs = GameConfig.NDCPTTManager.ThanThongs;
        var tiers = new[] { ETierSkill.DiaGiaiHa, ETierSkill.DiaGiaiTrung, ETierSkill.DiaGiaiThuong };
        var items = thanThongs
            .Where(i => tiers.Contains(i.eTierSkill))
            .ToArray();
        var item = items.Random();
        return new ItemRef()
        {
            Id = (ushort)item.Id,
            Type = ItemType.ThanThong,
            Count = 1,
        };
    }

    private ItemRef RandomCongPhapThienGiai()
    {
        var congPhaps = GameConfig.NDCPTTManager.CongPhaps;
        var tiers = new[] { ETierSkill.ThienGiaiHa, ETierSkill.ThienGiaiTrung, ETierSkill.ThienGiaiThuong };
        var items = congPhaps
            .Where(i => tiers.Contains(i.eTierSkill))
            .ToArray();
        var item = items.Random();
        return new ItemRef()
        {
            Id = (ushort)item.Id,
            Type = ItemType.CongPhap,
            Count = 1,
        };
    }

    private ItemRef RandomCongPhapDiaGiai()
    {
        var congPhaps = GameConfig.NDCPTTManager.CongPhaps;
        var tiers = new[] { ETierSkill.DiaGiaiHa, ETierSkill.DiaGiaiTrung, ETierSkill.DiaGiaiThuong };
        var items = congPhaps
            .Where(i => tiers.Contains(i.eTierSkill))
            .ToArray();
        var item = items.Random();
        return new ItemRef()
        {
            Id = (ushort)item.Id,
            Type = ItemType.CongPhap,
            Count = 1,
        };
    }

    private ItemRef RandomLinhDan(Tier tier)
    {
        var linhDan = GameConfig.LuyenDanResources.LinhDans
            .Where(i => i.Tier == tier).ToArray().Random();
        return new ItemRef()
        {
            Id = (ushort)linhDan.Id,
            Type = ItemType.LinhDan,
            Count = 1,
        };
    }

    private void CreateSessions()
    {
        var start = Utils.Utils.Now();
        var deltaTime = TimeSpan.FromMinutes(5);
        var sessionConfigs = new[]
        {
            new
            {
                Title = "Phiên thứ nhất",
                Status = AuctionSessionStatus.InProgress,
                Price = 4_000,
                Item = RandomThanThongDiaGiai()
            },
            new
            {
                Title = "Phiên thứ hai",
                Status = AuctionSessionStatus.NotStarted,
                Price = 2_000,
                Item = RandomLinhDan(Tier.TuPham)
            },
            new
            {
                Title = "Phiên thứ ba",
                Status = AuctionSessionStatus.NotStarted,
                Price = 5_000,
                Item = RandomCongPhapDiaGiai()
            },
            new
            {
                Title = "Phiên thứ tư",
                Status = AuctionSessionStatus.NotStarted,
                Price = 5_000,
                Item = RandomLinhDan(Tier.NguPham)
            },
            new
            {
                Title = "Phiên thứ năm",
                Status = AuctionSessionStatus.NotStarted,
                Price = 8_000,
                Item = RandomThanThongThienGiai()
            },
            new
            {
                Title = "Phiên thứ sáu",
                Status = AuctionSessionStatus.NotStarted,
                Price = 10_000,
                Item = RandomLinhDan(Tier.LucPham)
            },
            new
            {
                Title = "Phiên thứ bảy",
                Status = AuctionSessionStatus.NotStarted,
                Price = 10_000,
                Item = RandomCongPhapThienGiai()
            },
        };
        Sessions = new AuctionSession[sessionConfigs.Length];
        for (var i = 0; i < sessionConfigs.Length; i++)
        {
            var config = sessionConfigs[i];
            var end = start + deltaTime;
            Sessions[i] = new AuctionSession()
            {
                SessionTitle = config.Title,
                Status = config.Status,
                ItemRef = config.Item,
                DefaultPrice = config.Price,
                CurrentPrice = config.Price,
                MinPrice = MinPriceOf(config.Price),
                MaxPrice = MaxPriceOf(config.Price),
                StartTime = start.TimeOfDay,
                EndTime = end.TimeOfDay,
            };
            start = end;
        }
    }

    private int MinPriceOf(int price)
    {
        return (int)(price * 1.1f);
    }

    private int MaxPriceOf(int price)
    {
        return price * 2;
    }

    public TimeSpan GetRemainingTimeSession(AuctionSession session)
    {
        var now = Utils.Utils.Now().TimeOfDay;
        var remainingTime = session.EndTime - now;
        return remainingTime > TimeSpan.Zero ? remainingTime : TimeSpan.Zero;
    }

    public CurrentAuctionItem GetCurrentAuctionItem()
    {
        var current = CurrentSession;
        var playerLeading = PlayerModels.FirstOrDefault(x => x.Status == PlayerAuctionStatus.Leading);
        return new CurrentAuctionItem()
        {
            Item = current.ItemRef,
            TotalTime = current.EndTime - current.StartTime,
            RemainingTime = GetRemainingTimeSession(current),
            PlayerBid = playerLeading != null ? playerLeading.Name : string.Empty,
            CurrentPrice = current.CurrentPrice,
            MinPrice = current.MinPrice,
            MaxPrice = current.MaxPrice,
        };
    }

    private async Task HostRandomChatTask()
    {
        try
        {
            while (ThisState == State.Started)
            {
                var randomSecond = Random.Shared.NextDouble() * 10 + 7;
                await Task.Delay(TimeSpan.FromSeconds(randomSecond), cts.Token);
                HostRandomChat();
            }
        }
        catch (OperationCanceledException e)
        {
        }
    }

    private async Task Run()
    {
        _ = HostRandomChatTask();
        try
        {
            while (ThisState == State.Started)
            {
                var current = CurrentSession;
                var delayTime = GetRemainingTimeSession(current);
                await Task.Delay(delayTime, cts.Token);
                FinalPrice(current);
                var newIndex = CurrentSessionIndex + 1;
                lock (currentSessionLock)
                {
                    if (newIndex >= Sessions.Length)
                    {
                        ThisState = State.Ended;
                        OnEnded().GetAwaiter().GetResult();
                    }
                    else
                    {
                        CurrentSessionIndex = newIndex;
                        current = CurrentSession;
                        current.Status = AuctionSessionStatus.InProgress;
                        var item = GetCurrentAuctionItem();
                        SendAll(MessageTag.NewAuctionSession, item);
                    }
                }
            }
        }
        catch (OperationCanceledException e)
        {
        }
    }

    public async void SendResultToLeading(PlayerAuctionModel player, AuctionSession session)
    {
        var amount = player.Amount;
        CharacterData? charData = null!;
        if (Players.TryGetValue(player.PlayerId, out var onlinePlayer))
        {
            charData = onlinePlayer.GetCharacterData();
        }
        else
        {
            var characterOnlines = TcpServer.Instance.GetCharsOnline();
            charData = characterOnlines.FirstOrDefault(x => x.Id == player.CharId);
        }

        if (charData == null)
        {
            try
            {
                await using var db = await Program.CreateDbAsync();
                charData = await db.Characters
                    .AsNoTracking()
                    .Include(x => x.InventoryManager)
                    .FirstOrDefaultAsync(x => x.Id == player.CharId);
                if (charData == null)
                {
                    return;
                }

                var inventory = charData.InventoryManager;
                await inventory.Load(db);
                AddToInventory(session.ItemRef, inventory);
                await inventory.SaveChanges(db);
                await charData.AddSpendMoneyVipOffline(amount, SpendFor.Auction, db);
                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }
        else
        {
            var client = charData.Client;
            AuctionReward reward = null!;
            lock (client.CharacterLock)
            {
                reward = AddToInventory(session.ItemRef, charData.InventoryManager);
            }
            reward.Content = Strings.AuctionRewardReceived;
            charData.AddSpendMoneyVipOnline(amount, SpendFor.Auction);
            client.MemSend(MessageTag.AuctionReward, reward);
            client.MemSend(MessageTag.Notify, Strings.AuctionRewardReceived);
        }
    }

    private AuctionReward AddToInventory(ItemRef itemRef, InventoryManager inventory)
    {
        List<FurnaceData> furnaceDatas = null!;
        if (itemRef.Type == ItemType.Furnace)
        {
            furnaceDatas = new List<FurnaceData>();
            for (int i = 0; i < itemRef.Count; i++)
            {
                var furnaceData = new FurnaceData()
                {
                    Id = itemRef.Id,
                    Durability = 100
                };
                inventory.AddFurnace(furnaceData);
                furnaceDatas.Add(furnaceData);
            }
        }
        else
        {
            inventory.Add(itemRef);
        }

        return new AuctionReward()
        {
            Item = itemRef,
            FurnaceDatas = furnaceDatas
        };
    }

    private void FinalPrice(AuctionSession session)
    {
        session.Status = AuctionSessionStatus.Ended;
        lock (playerModelsLock)
        {
            var leading = GetLeading();
            if (leading != null)
            {
                session.HighestBidder = new HighestBidder()
                {
                    CharId = leading.CharId,
                    Amount = leading.Amount,
                    Name = leading.Name,
                };
                SendResultToLeading(leading, session);
                HostChat(string.Format(Strings.AuctionHostSessionEnded, leading.Name,
                    Utils.Utils.FormatCount(leading.Amount)));
            }

            foreach (var player in PlayerModels)
            {
                player.ResetNewSession();
            }
        }
    }

    private PlayerAuctionModel GetLeading()
    {
        var leading = PlayerModels.FirstOrDefault(x => x.Status == PlayerAuctionStatus.Leading);
        return leading;
    }

    public void HostChat(string message)
    {
        SendAll(MessageTag.HostChat, message);
    }

    private void HostRandomChat()
    {
        var chats = Strings.AuctionHostRandomChats;
        if (chats.Length == 0)
        {
            return;
        }

        HostChat(chats.Random());
    }

    private async Task RefundMoney(PlayerAuctionModel playerModel)
    {
        CharacterData charData = null!;
        var amount = playerModel.Amount;
        if (Players.TryGetValue(playerModel.PlayerId, out var onlinePlayer))
        {
            charData = onlinePlayer.GetCharacterData();
        }
        else
        {
            charData = TcpServer.Instance
                .GetCharsOnline()
                .FirstOrDefault(x => x.Id == playerModel.CharId);
        }

        if (charData == null)
        {
            await using var db = await Program.CreateDbAsync();
            charData = await db.Characters.FirstOrDefaultAsync(x => x.Id == playerModel.CharId);
            if (charData != null)
            {
                charData.SetMoneyVip(charData.MoneyVip + amount);
                await db.SaveChangesAsync();
            }
        }
        else
        {
            var client = charData.Client;
            lock (client.CharacterLock)
            {
                charData.SetMoneyVip(charData.MoneyVip + amount);
                client.MemSend(MessageTag.Notify, Strings.AuctionBidRefunded);
                client.MemSend(MessageTag.RefundAuctionBid, new RefundAuctionBid()
                {
                    RefundMoney = amount,
                    NewMoneyVip = charData.MoneyVip
                });
            }
        }
    }

    public AuctionBidResult PlayerBid(PlayerAuction player, int amount)
    {
        lock (currentSessionLock)
        {
            var currentSession = CurrentSession;
            if (currentSession.Status == AuctionSessionStatus.Ended)
            {
                throw new InvalidException(Strings.AuctionSessionEnded);
            }

            var charData = player.GetCharacterData();
            var model = player.Model;
            var total = charData.MoneyVip + model.Amount;
            var remaining = total - amount;
            if (remaining < 0)
            {
                throw new InvalidException(Strings.NotEnoughMoneyVip);
            }

            if (amount < currentSession.MinPrice || amount > currentSession.MaxPrice)
            {
                throw new InvalidException(Strings.AuctionBidAmountInvalid);
            }

            currentSession.CurrentPrice = amount;
            currentSession.MinPrice = MinPriceOf(amount);
            currentSession.MaxPrice = MaxPriceOf(amount);
            var leading = GetLeading();
            if (leading != null)
            {
                if (leading.CharId != model.CharId)
                {
                    _ = RefundMoney(leading);
                    leading.Status = PlayerAuctionStatus.HasBid;
                    leading.Amount = 0;
                }
            }

            charData.SetMoneyVip(remaining);
            model.Amount = amount;
            model.Status = PlayerAuctionStatus.Leading;
            lock (playerModelsLock)
            {
                if (!IsInPlayerModels(model))
                {
                    var index = FindIndexCanReplace();
                    if (index >= 0)
                    {
                        var oldModel = PlayerModels[index];
                        PlayerModels[index] = model;
                        SendAll(MessageTag.ReplaceAuctionPlayer, new ReplaceAuctionPlayer()
                        {
                            Player = model,
                            PlayerId = oldModel.PlayerId,
                        });
                    }
                    else
                    {
                        Log.Error($"Cannot replace player {model.CharId} with {model.PlayerId}");
                    }
                }
            }

            var highestBidChanged = new HighestBidChanged()
            {
                PlayerId = model.PlayerId,
                Amount = amount,
                MinAmount = currentSession.MinPrice,
                MaxAmount = currentSession.MaxPrice,
            };
            SendAll(MessageTag.HighestBidChanged, highestBidChanged);
            HostChat(string.Format(Strings.AuctionHostBidPlaced, model.Name, Utils.Utils.FormatCount(amount)));
            return new AuctionBidResult()
            {
                Success = true,
                Amount = amount,
                NewMoneyVip = charData.MoneyVip,
            };
        }
    }

    public void SendAll<T>(MessageTag tag, T value)
    {
        var data = PackUtils.PackMessage(tag, value);
        var players = Players.Values.ToArray();
        foreach (var player in players)
        {
            player.Send(data);
        }
    }

    public bool IsInPlayerModels(PlayerAuctionModel model)
    {
        return PlayerModels.Any(p => p.CharId == model.CharId);
    }

    private int FindIndexCanReplace()
    {
        var finder = PlayerModels
            .Select((p, i) => new
            {
                Data = p,
                Index = i,
            })
            .Where(x => x.Data.Status == PlayerAuctionStatus.Guest)
            .OrderBy(x => x.Data.JoinedAt)
            .FirstOrDefault();
        if (finder != null)
        {
            return finder.Index;
        }

        return -1;
    }

    public void OnNewPlayerJoined(PlayerAuction player)
    {
        var model = player.Model;
        if (model == null)
        {
            var charData = player.GetCharacterData();
            var index = Array.FindIndex(poolPlayerModels, p => p.CharId == charData.Id);
            if (index < 0)
            {
                index = Array.FindIndex(PlayerModels, p => p.CharId == charData.Id);
                if (index < 0)
                {
                    var id = GetNewId();
                    model = new PlayerAuctionModel(charData, id);
                }
                else
                {
                    model = PlayerModels[index];
                }
            }
            else
            {
                model = poolPlayerModels[index];
            }
        }

        model.JoinedAt = DateTime.UtcNow;
        player.SetPlayerAuctionModel(model);
        lock (playerModelsLock)
        {
            if (!IsInPlayerModels(model))
            {
                var index = FindIndexCanReplace();
                if (index >= 0)
                {
                    var oldModel = PlayerModels[index];
                    PlayerModels[index] = model;
                    SendAll(MessageTag.ReplaceAuctionPlayer, new ReplaceAuctionPlayer()
                    {
                        Player = model,
                        PlayerId = oldModel.PlayerId,
                    });
                }
                else
                {
                    Log.Error($"Cannot replace player {model.CharId} with {model.PlayerId}");
                }
            }
        }

        Players.TryAdd(model.PlayerId, player);
        HostChat(string.Format(Strings.AuctionHostPlayerJoined, model.Name));
    }

    public int GetNewId()
    {
        lock (idLock)
        {
            return ++playerIdCounter;
        }
    }

    public void PlayerExit(PlayerAuction player)
    {
        Players.TryRemove(player.Model.PlayerId, out _);
    }

    public AuctionInfo GetInfo()
    {
        return new AuctionInfo()
        {
            Content = content,
            IsOpen = ThisState == State.Started,
            AuctionSessions = Sessions
        };
    }

    public async Task ForceStop()
    {
        if (ThisState != State.Started)
        {
            return;
        }

        ThisState = State.Ended;
        await cts.CancelAsync();
        cts.Dispose();
        var leading = GetLeading();
        if (leading != null)
        {
            await RefundMoney(leading);
        }

        await OnEnded();
    }

    private async Task OnEnded()
    {
        await using var db = await Program.CreateDbAsync();
        await SaveHistory(db);
        OnAuctionEnded?.Invoke();
    }

    public async Task SaveHistory(AppDbContext db)
    {
        var history = new AuctionHistory()
        {
            StartTime = StartTime,
            EndTime = DateTime.UtcNow,
            Sessions = Sessions
        };
        await db.AuctionHistories.AddAsync(history);
        await db.SaveChangesAsync();
    }

    public void SetContent(string content)
    {
        this.content = content;
    }

    public enum State
    {
        NotStarted,
        Started,
        Ended
    }
}