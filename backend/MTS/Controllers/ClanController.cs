using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MTS.Services.Clans;
using SixLabors.ImageSharp;

namespace MTS.Controllers;

[Authorize]
[Route("clans")]
public sealed class ClanController(ClanImageService images) : Controller
{
    [HttpPost("{clanId:int}/image")]
    [RequestSizeLimit(ClanImageService.MaxBytes + 65536)]
    [RequestFormLimits(MultipartBodyLengthLimit = ClanImageService.MaxBytes)]
    public async Task<IActionResult> UploadImage(int clanId, [FromForm] IFormFile file, CancellationToken cancellationToken)
    {
        if (!TryGetId(out var charId)) return Unauthorized();
        if (file == null || file.Length <= 0 || file.Length > ClanImageService.MaxBytes)
            return BadRequest("InvalidImageSize");
        try
        {
            using var stream = new MemoryStream();
            await file.CopyToAsync(stream, cancellationToken);
            await images.Upload(charId, clanId, stream.ToArray(), cancellationToken);
            return NoContent();
        }
        catch (ClanRuleException ex)
        {
            return StatusCode(ex.Code == "Forbidden" ? 403 : ex.Code == "StaleVersion" ? 409 : 400, ex.Code);
        }
        catch (Exception ex) when (ex is UnknownImageFormatException or InvalidImageContentException or NotSupportedException)
        {
            return BadRequest("InvalidImage");
        }
    }
}
