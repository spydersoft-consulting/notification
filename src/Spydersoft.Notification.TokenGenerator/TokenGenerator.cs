using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace Spydersoft.Notification.TokenGenerator;

public static class TokenGenerator
{
    public const string DefaultTestUserId = "notification-test-user";

    /// <summary>
    /// Generates a test JWT. A "machine" token omits notification:read and represents a
    /// backend service (e.g. PitStop's recall-check job) creating notifications for other
    /// users. A "readOnly" token omits notification:write, representing a user token with no
    /// create/write grant. See plans/notifications/service-spec.md#authorization.
    /// </summary>
    public static string Generate(string base64Key, string userId, bool machine = false, bool readOnly = false)
    {
        var key = new SymmetricSecurityKey(Convert.FromBase64String(base64Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim> { new(JwtRegisteredClaimNames.Sub, userId) };

        if (!readOnly)
        {
            claims.Add(new("scope", "notification:write"));
        }

        if (!machine)
        {
            claims.Add(new("scope", "notification:read"));
        }

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddDays(365),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
