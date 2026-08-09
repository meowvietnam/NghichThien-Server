
using Google.Apis.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MTS.Api.Models;
using MTS.Api.Services;
using MTS.Data;
using MTS.Exceptions;
using MTS.Localization;
using MTS.Network;
using MTS.Network.Models;
using MTS.Services;

namespace MTS.Controllers;
public record LoginWithGoogleRequest(string IdToken);
[Route("/auth")]
public class AuthController : Controller
{
    private readonly JwtService jwtService;
    private readonly AppDbContext db;
    public AuthController(JwtService jwtService, AppDbContext db)
    {
        this.jwtService = jwtService;
        this.db = db;
    }
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetMe()
    {
        try
        {
            if (!TryGetId(out int charId))
            {
                return Unauthorized();
            }
            var charData = await db.Characters
                .Include(x => x.Progression)
                .Include(x => x.Stats)
                .FirstOrDefaultAsync(x => x.Id == charId);
            if (charData == null)
            {
                return Unauthorized();
            }
            var character = new Character(charData);
            return Success(character);
        }
        catch (Exception e)
        {
            return Fail(e.Message);
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginWithUsernamePassword request)
    {
        try
        {
            var loginResult = await request.Login(null);
            if (!loginResult.Success)
            {
                throw new InvalidException(string.IsNullOrWhiteSpace(loginResult.Message)
                    ? Strings.UsernameOrPasswordInvalid
                    : loginResult.Message);
            }
            var charData = loginResult.UserData.User.CharacterDatas.FirstOrDefault();
            if (charData == null)
            {
                throw new InvalidException(Strings.CharacterNotFound);
            }
            var jwtToken = jwtService.GenerateToken(charData);
            return Success(new
            {
                Message = loginResult.Message,
                Token = jwtToken
            });
        }
        catch (Exception e)
        {
            return Fail(e.Message);
        }
    }
    [HttpPost("login-with-google")]
    public async Task<IActionResult> LoginWithGoogle([FromBody] LoginWithGoogleRequest request)
    {
        try
        {
            var idToken = request.IdToken;
            GoogleJsonWebSignature.Payload payload;
            try
            {
                payload = await GoogleJsonWebSignature.ValidateAsync(idToken);
            }
            catch
            {
                throw new InvalidException(Strings.GoogleLoginInvalid);
            }
            var email = payload.Email;
            var name = payload.Name;
            var account = await db.GoogleAccounts.FirstOrDefaultAsync(x => x.Email == email);
            if (account == null)
            {
                throw new InvalidException(Strings.GoogleAccountNotFound);
            }
            var userId = account.UserId;
            var charData = await db.Characters.FirstOrDefaultAsync(x => x.UserId == userId);
            if (charData == null)
            {
                throw new InvalidException(Strings.CharacterNotFound);
            }
            var jwtToken = jwtService.GenerateToken(charData);
            var message = $"Đăng nhập thành công với tài khoản Google {name}!";
            return Success(new
            {
                Message = message,
                Token = jwtToken,
            });
        }
        catch (Exception e)
        {
            return Fail(e.Message);
        }
    }
}
