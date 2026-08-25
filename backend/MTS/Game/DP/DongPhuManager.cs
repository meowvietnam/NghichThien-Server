using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using MTS.Data;
using MTS.Exceptions;
using MTS.Models;
using MTS.Network;
using MTS.Network.ClientState;
using MTS.Network.Models;

namespace MTS.Game.DP;

public static class DongPhuManager
{
    private static ConcurrentDictionary<int, DongPhuObserver> observers = new();

    static DongPhuManager()
    {
        DbEvent.OnUserDeleted += OnUserDeleted;
    }

    public static void OnRemoveState(DongPhuObserver observer, DongPhuState state)
    {
        if (observer.States.Count == 0)
        {
            var id = observer.DongPhu.CharId;
            observers.TryRemove(id, out _);
        }
    }

    public static async Task<(DongPhuObserver observer, DongPhuState state)>
        JoinDongPhuObserver(int charId, Client client)
    {
        var charData = client.CharacterData;
        if (charData.Progression.MaxLevel < 4)
        {
            throw new InvalidException("Yêu cầu đạt Trúc cơ sơ kì");
        }

        await using var db = await Program.CreateDbAsync();
        var myCharId = charData.Id;
        var observer = await FindOrCreateDongPhuObserver(charId, db);
        DongPhuState state = null!;
        if (charId == myCharId)
        {
            var dongPhu = observer.DongPhu;
            state = new DongPhuState(client, observer, GameConfig.DongPhuConfig, dongPhu.Level,
                dongPhu.CurrentWateringHelpCount);
        }
        else
        {
            var data = await db.DongPhus.Where(x => x.CharId == myCharId)
                .Select(x => new { x.Level, x.CurrentWateringHelpCount })
                .FirstOrDefaultAsync();
            if (data == null)
            {
                throw new InvalidException("Động phủ của bạn chưa mở");
            }

            state = new DongPhuState(client, observer, GameConfig.DongPhuConfig, data.Level,
                data.CurrentWateringHelpCount);
        }

        observer.AddState(state);
        return (observer, state);
    }

    private static async Task<DongPhuObserver> FindOrCreateDongPhuObserver(int charId, AppDbContext db)
    {
        if (observers.TryGetValue(charId, out var observer))
        {
            return observer;
        }

        var dongPhu = await db.DongPhus.FirstOrDefaultAsync(x => x.CharId == charId);
        if (dongPhu == null)
        {
            throw new InvalidException("Động phù không tồn tại");
        }

        observer = new DongPhuObserver(dongPhu);
        return observer;
    }

    private static void OnUserDeleted(UserData userData)
    {
        foreach (var charData in userData.CharacterDatas)
        {
            if (observers.TryRemove(charData.Id, out var observer))
            {
                observer.OnUserDeleted();
            }
        }
    }

    public static void OnDongPhuLiked(DongPhu dongPhu, DongPhuLike like)
    {
        if (observers.TryGetValue(dongPhu.CharId, out var observer))
        {
            observer.OnDongPhuLiked(like);
        }
    }

    public static void OnWateringHelp(int charId, AddWateringHelp addWateringHelp)
    {
        if (observers.TryGetValue(charId, out var observer))
        {
            observer.OnWateringHelp(addWateringHelp);
        }
    }

    public static void OnResetDaily(DongPhu dongPhu)
    {
        foreach (var observer in observers.Values)
        {
            if (observer.UpdateStateDailyReset(dongPhu))
            {
                break;
            }
        }

        if (observers.TryGetValue(dongPhu.CharId, out var obs))
        {
            obs.OnMyResetDaily(dongPhu);
        }
    }

    public static async Task UpgradeLevelDongPhu(int charId)
    {
        DongPhu dongPhu = null!;
        if (observers.TryGetValue(charId, out var observer))
        {
            dongPhu = observer.DongPhu;
            dongPhu.TryUpgrade();
        }
        else
        {
            await using var db = await Program.CreateDbAsync();
            var config = GameConfig.DongPhuConfig;
            dongPhu = await db.DongPhus.FirstOrDefaultAsync(x => x.CharId == charId);
            if (dongPhu == null)
            {
                dongPhu = new DongPhu()
                {
                    CharId = charId,
                    Level = config.Levels.First().Level,
                };
                dongPhu.FirstInit(config);
                await db.DongPhus.AddAsync(dongPhu);
                await db.SaveChangesAsync();
            }
        }
    }

    public static async Task SaveChanges()
    {
        foreach (var observer in observers.Values)
        {
            await observer.SaveChanges();
        }
    }
}