using Microsoft.EntityFrameworkCore;
using MTS.Data;
using MTS.Network;
using MTS.Network.Models;
using Network;
using Network.Extensions;

namespace MTS.Models;

public class MailManager
{
    private readonly CharacterData characterData;
    private readonly ISender<MessageTag>? sender;

    public MailManager(CharacterData characterData, ISender<MessageTag>? sender)
    {
        this.characterData = characterData;
        this.sender = sender;
    }

    public async Task<GetMailsResult> GetMails(long startId, int pageSize)
    {
        pageSize = Math.Clamp(pageSize, 10, 100);
        var charId = characterData.Id;
        await using var db = await Program.CreateDbAsync();
        var mails = await db.Mails
            .AsNoTracking()
            .Where(x => x.CharId == charId && x.Id < startId)
            .OrderByDescending(x => x.Id)
            .Take(pageSize + 1)
            .ToListAsync();
        var hasNext = mails.Count > pageSize;
        if (hasNext)
        {
            mails.RemoveAt(mails.Count - 1);
        }

        return new GetMailsResult
        {
            Success = true,
            Mails = mails,
            HasNext = hasNext
        };
    }

    public async Task<Mail> SendMail(Mail mail)
    {
        mail.CharId = characterData.Id;
        await using var db = await Program.CreateDbAsync();
        var entity = await db.Mails.AddAsync(mail);
        await db.SaveChangesAsync();
        sender?.Send(MessageTag.NewMail, entity.Entity);
        return entity.Entity;
    }

    public async Task<long> ReadMail(long id)
    {
        try
        {
            var charId = characterData.Id;
            await using var db = await Program.CreateDbAsync();
            await db.Mails
                .Where(x => x.Id == id && x.CharId == charId)
                .ExecuteUpdateAsync(s => s.SetProperty(u => u.IsRead, true));
            return id;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return -1;
        }
    }

    public async Task<long> RemoveMail(long id)
    {
        try
        {
            var charId = characterData.Id;
            await using var db = await Program.CreateDbAsync();
            await db.Mails.Where(x => x.Id == id && x.CharId == charId)
                .ExecuteDeleteAsync();
            return id;
        }
        catch (Exception ex)
        {
            return -1;
        }
    }

    public async Task<List<long>> RemoveAllMails()
    {
        var charId = characterData.Id;
        await using var db = await Program.CreateDbAsync();
        var mails = await db.Mails
            .Where(x => x.IsRead && x.CharId == charId)
            .ToListAsync();

        var removeIds =  mails
            .Where(x => x.IsClaim || !x.Rewards.Any())
            .Select(x => x.Id)
            .ToList();
        if (removeIds.Count > 0)
        {
            await db.Mails
                .Where(x => removeIds.Contains(x.Id))
                .ExecuteDeleteAsync();
        }

        return removeIds;
    }

    public async Task<RewardResult?> ClaimMail(long id)
    {
        await using var db = await Program.CreateDbAsync();
        var mail = await db.Mails.FirstOrDefaultAsync(x => x.Id == id);
        if (mail == null || mail.IsClaim) return null;
        var rewards = mail.Rewards;
        foreach (var mailReward in rewards)
        {
            mailReward.ClaimReward(characterData);
        }

        mail.IsClaim = true;
        await db.SaveChangesAsync();
        return new RewardResult { Mails = new List<long> { id }, Rewards = rewards };
    }

    public async Task<RewardResult?> ClaimAllMails()
    {
        await using var db = await Program.CreateDbAsync();
        var neverClaims = await db.Mails.Where(x => !x.IsClaim).ToListAsync();
        if (!neverClaims.Any()) return null;
        var mailIds = neverClaims.Select(x => x.Id).ToList();
        var rewards = neverClaims.SelectMany(x => x.Rewards).ToList();
        foreach (var mailReward in rewards)
        {
            mailReward.ClaimReward(characterData);
        }

        foreach (var mail in neverClaims) mail.IsClaim = true;
        await db.SaveChangesAsync();
        return new RewardResult { Mails = mailIds, Rewards = rewards };
    }
}