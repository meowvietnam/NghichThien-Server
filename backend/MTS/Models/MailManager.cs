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
    private readonly ISender<MessageTag> sender;
    private bool isLoaded;

    public MailManager(CharacterData characterData, ISender<MessageTag> sender)
    {
        this.characterData = characterData;
        this.sender = sender;
    }

    public async Task<List<Mail>> GetMails()
    {
        if (!isLoaded)
        {
            await using var db = await Program.CreateDbAsync();
            var mails = await db.Mails
                .AsNoTracking()
                .Where(x => x.CharId == characterData.Id)
                .ToListAsync();
            characterData.Social.Mails = mails;
            isLoaded = true;
        }
        return characterData.Social.Mails;
    }

    public async Task SendMail(Mail mail)
    {
        mail.CharId = characterData.Id;
        await using var db = await Program.CreateDbAsync();
        var entity = await db.Mails.AddAsync(mail);
        await db.SaveChangesAsync();
        sender.Send(MessageTag.NewMail, entity.Entity);
    }

    public int ReadMail(int id)
    {
        try
        {
            var mail = characterData.Social.Mails.FirstOrDefault(x => x.Id == id);
            if (mail == null || mail.IsRead) return -1;
            mail.IsRead = true;
            _ = Task.Run(async () =>
            {
                await using var db = await Program.CreateDbAsync();
                await db.Mails
                    .Where(x => x.Id == id)
                    .ExecuteUpdateAsync(s => s.SetProperty(u => u.IsRead, true));
            });
            return id;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return -1;
        }
    }

    public int RemoveMail(int id)
    {
        var mail = characterData.Social.Mails.FirstOrDefault(x => x.Id == id);
        if (mail == null) return -1;
        _ = Task.Run(async () =>
        {
            await using var db = await Program.CreateDbAsync();
            await db.Mails.Where(x => x.Id == id).ExecuteDeleteAsync();
        });
        characterData.Social.Mails.Remove(mail);
        return id;
    }

    public List<int> RemoveAllMails()
    {
        var removes = characterData.Social.Mails
            .Where(x => x.IsRead && (x.Rewards.Count == 0 || x.IsClaim))
            .ToList();
        var removeIds = removes.Select(x => x.Id).ToList();
        if (removes.Any())
        {
            characterData.Social.Mails.RemoveAll(x => removeIds.Contains(x.Id));
            _ = Task.Run(async () =>
            {
                await using var db = await Program.CreateDbAsync();
                await db.Mails.Where(x => removeIds.Contains(x.Id)).ExecuteDeleteAsync();
            });
        }
        return removeIds;
    }

    public async Task<RewardResult?> ClaimMail(int id, InventoryManager inventory)
    {
        var mail = characterData.Social.Mails.FirstOrDefault(x => x.Id == id);
        if (mail == null || mail.IsClaim) return null;
        var result = inventory.AddToInventory(mail.Rewards);
        mail.IsClaim = true;
        _ = Task.Run(async () =>
        {
            await using var db = await Program.CreateDbAsync();
            await db.Mails
                .Where(x => x.Id == id)
                .ExecuteUpdateAsync(s => s.SetProperty(u => u.IsClaim, true));
        });
        return new RewardResult { Mails = new List<int> { id }, RewardItems = result };
    }

    public async Task<RewardResult?> ClaimAllMails(InventoryManager inventory)
    {
        var neverClaims = characterData.Social.Mails.Where(x => !x.IsClaim).ToList();
        if (!neverClaims.Any()) return null;
        var mailIds = neverClaims.Select(x => x.Id).ToList();
        var rewards = neverClaims.SelectMany(x => x.Rewards).ToList();
        var result = inventory.AddToInventory(rewards);
        _ = Task.Run(async () =>
        {
            await using var db = await Program.CreateDbAsync();
            await db.Mails
                .Where(x => mailIds.Contains(x.Id))
                .ExecuteUpdateAsync(s => s.SetProperty(u => u.IsClaim, true));
        });
        foreach (var mail in neverClaims) mail.IsClaim = true;
        return new RewardResult { Mails = mailIds, RewardItems = result };
    }
}
