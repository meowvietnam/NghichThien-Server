using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using MTS.Models;

namespace MTS.Api.Services;

public class JwtService
{
    private readonly IConfiguration _configuration;

    public JwtService(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    public string GenerateToken(CharacterData charData)
    {
        var jwt = _configuration.GetSection("Jwt");
        var key = jwt["Key"]!;
        var issuer = jwt["Issuer"];
        var audience = jwt["Audience"];
        var expiresInMinutes = int.Parse(jwt["ExpiresInMinutes"]!);
        var claims = new List<Claim>
        {
            new ("userId", charData.UserId.ToString()),
            new("charId", charData.Id.ToString()),
            new("name", charData.Name),
        };
        var securityKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(key)
        );
        var credentials = new SigningCredentials(
            securityKey,
            SecurityAlgorithms.HmacSha256
        );
        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiresInMinutes),
            signingCredentials: credentials
        );
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}