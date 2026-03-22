using System.Diagnostics;
using JetBrains.Profiler.Api;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using MTS.Game;
using MTS.Game.Auth;
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
    [HttpPost("/apply-stats")]
    public async Task<IActionResult> ApplyStats(IFormFile file)
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
            var selection = service.GetSelection(models);
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
    [HttpPost("/create-skin")]
    public async Task<IActionResult> CreateSkin(IFormFile file)
    {
        try
        {
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            var data = ms.ToArray();
            var reader = new Reader(data);
            var request = reader.Read<CreateSkinRequest>();
            var service = new CreateCharacterService();
            service.CreateSkin(request);
            var c = service.Char;
            return Ok(new
            {
                Request = request,
                Response = new CreateSkinRequest()
                {
                    Gender = c.Gender,
                    Skins = c.Skins,
                    Colors = c.Colors,
                }
            });
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }

    [HttpPost("/login-or-create")]
    public async Task<IActionResult> Login([FromBody] string username)
    {
        try
        {
            var service = new AuthService();
            var userData = service.CreateOrLogin(username);
            return Ok(userData);
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }

    private async Task<Reader> CreateReader(IFormFile file)
    {
        using var ms = new MemoryStream();
        await file.CopyToAsync(ms);
        var data = ms.ToArray();
        var reader = new Reader(data);
        return reader;
    }
    [HttpPost("/create-character")]
    public async Task<IActionResult> CreateCharacter([FromForm] CreateCharacterRequest request)
    {
        try
        {
            var username = request.Username;
            var name = request.Name;
            var fileSkin = request.FileSkin;
            var fileStats = request.FileStats;
            var authService = new AuthService();

            var sw = Stopwatch.StartNew();
            
            var user = authService.CreateOrLogin(username);
            Console.WriteLine($"[1] authService.CreateOrLogin: {sw.ElapsedMilliseconds} ms");
            sw.Restart();

            var reader = await CreateReader(fileSkin);
            Console.WriteLine($"[2] await CreateReader(fileSkin): {sw.ElapsedMilliseconds} ms");
            sw.Restart();

            var createSkinRequest = reader.Read<CreateSkinRequest>();
            Console.WriteLine($"[3] reader.Read<CreateSkinRequest>(): {sw.ElapsedMilliseconds} ms");
            sw.Restart();

            reader = await CreateReader(fileStats);
            Console.WriteLine($"[4] await CreateReader(fileStats): {sw.ElapsedMilliseconds} ms");
            sw.Restart();

            var models = reader.ReadArray<SelectionModel>().ToList();
            Console.WriteLine($"[5] reader.ReadArray<SelectionModel>().ToList(): {sw.ElapsedMilliseconds} ms");
            sw.Restart();

            var service = new CreateCharacterService();
            Console.WriteLine($"[6] new CreateCharacterService(): {sw.ElapsedMilliseconds} ms");
            sw.Restart();

            user = service.Create(user.Id, name, createSkinRequest, models);
            Console.WriteLine($"[7] service.Create(user.Id, name, createSkinRequest, models): {sw.ElapsedMilliseconds} ms");
            sw.Stop();
            return Ok(user);
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }
}
public class CreateCharacterRequest
{
    public string Username { get; set; }
    public string Name { get; set; }

    public IFormFile FileSkin { get; set; }
    public IFormFile FileStats { get; set; }
}