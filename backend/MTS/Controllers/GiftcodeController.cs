using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MTS.Data;
using MTS.Exceptions;
using MTS.Localization;
using MTS.Models;

namespace MTS.Controllers;

public record ClaimGiftCodeRequest(string Code);

[Route("/giftcode")]
public class GiftcodeController : Controller
{
    private readonly AppDbContext db;

    public GiftcodeController(AppDbContext db)
    {
        this.db = db;
    }

    [HttpPost("claim")]
    [Authorize]
    public async Task<IActionResult> Claim([FromBody] ClaimGiftCodeRequest request)
    {
        try
        {
            if (!TryGetId(out var charId))
            {
                throw new InvalidException(Strings.NotLoggedIn);
            }

            var code = request.Code?.Trim();
            if (string.IsNullOrWhiteSpace(code))
            {
                throw new InvalidException(Strings.GiftcodeInvalid);
            }

            var giftCode = await db.GiftCodes
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Code == code);
            if (giftCode == null)
            {
                throw new InvalidException(Strings.GiftcodeInvalid);
            }
            if (giftCode.CanUse - giftCode.TotalUsed <= 0)
            {
                throw new InvalidException(Strings.GiftcodeOutOfUses);
            }
            var nowUtc = DateTime.UtcNow;
            if (nowUtc > giftCode.ExpireDate)
            {
                throw new InvalidException(Strings.GiftcodeExpired);
            }

            var used = await db.GiftcodeUseds.AnyAsync(x => x.CodeId == giftCode.Id && x.CharId == charId);
            if (used)
            {
                throw new InvalidException(Strings.GiftcodeAlreadyUsed);
            }

            var charData = await db.Characters.FirstOrDefaultAsync(c => c.Id == charId);
            if (charData == null)
            {
                throw new InvalidException(Strings.CharacterNotFound);
            }

            Mail mail = null!;
            var strategy = db.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await db.Database.BeginTransactionAsync();
                var updatedCount = await db.GiftCodes
                    .Where(x => x.Id == giftCode.Id
                                && x.TotalUsed < x.CanUse
                                && x.ExpireDate >= nowUtc)
                    .ExecuteUpdateAsync(s => s.SetProperty(x => x.TotalUsed, x => x.TotalUsed + 1));
                if (updatedCount == 0)
                {
                    throw new InvalidException(Strings.GiftcodeUnavailable);
                }

                mail = new Mail()
                {
                    CharId = charId,
                    Title = string.Format(Strings.GiftcodeMailTitle, giftCode.Name),
                    Content = string.Format(Strings.GiftcodeMailContent, giftCode.Name, giftCode.Description),
                    Rewards = giftCode.Rewards,
                };
                var giftCodeUsed = new GiftcodeUsed()
                {
                    CharId = charId,
                    CodeId = giftCode.Id,
                };
                await db.GiftcodeUseds.AddAsync(giftCodeUsed);
                await db.Mails.AddAsync(mail);
                try
                {
                    await db.SaveChangesAsync();
                }
                catch (DbUpdateException ex) when (ex.Entries.Any(x => x.Entity is GiftcodeUsed))
                {
                    throw new InvalidException(Strings.GiftcodeAlreadyUsed);
                }

                await transaction.CommitAsync();
            });
            var result = new
            {
                Message = Strings.GiftcodeClaimSuccess,
                Mail = mail,
            };
            return Success(result);
        }
        catch (Exception e)
        {
            return Fail(e.Message);
        }
    }
}
