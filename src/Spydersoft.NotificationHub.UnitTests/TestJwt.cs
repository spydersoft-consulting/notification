using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace Spydersoft.NotificationHub.UnitTests;

internal static class TestJwt
{
    // Matches the Auth:TestKey configured on the WebApplicationFactory below.
    public const string SigningKeyBase64 = "jRv3YFPH/19t9t5CgsEFgAkykfW5bQhHmceMprLgzlQ=";

    public static string Create(string userId)
    {
        var key = new SymmetricSecurityKey(Convert.FromBase64String(SigningKeyBase64));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            claims: [new Claim(ClaimTypes.NameIdentifier, userId)],
            signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
