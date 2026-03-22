using Microsoft.AspNetCore.Mvc;
using MTS.Game;
using MTS.Game.CreateCharacter;
using Network;
using Network.Extensions;

namespace MTS.Controllers;
[Route("/game")]
public class GameController : ControllerBase
{
    [HttpGet("/character-template")]
    public IActionResult GetCharacterTemplate()
    {
        return Ok(GameConfig.CharacterTemplate);
    }
    [HttpPost("/step-config")]
    public IActionResult GetStepConfig()
    {
        return Ok(GameConfig.StepConfig);
    }
    [HttpPost("/upload")]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        try
        {
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            var data = ms.ToArray();
            var reader = new Reader(data);
            var models = reader.ReadArray<SelectionModel>().ToList();
            var service = new CreateCharacterService();
            service.ApplyStats(models);
            var selection = service.Test(models);
            return Ok(new
            {
                Char = service.Char,
                Selection = selection,
            });
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }
}