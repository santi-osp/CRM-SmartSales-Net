using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using SmartSales.Contacts.Domain.Entities;

namespace SmartSales.Contacts.Infrastructure.Security;

public sealed class TokenIssuer(JwtConfiguration jwt, TimeProvider clock)
{
    public string Issue(User user)
    {
        var now = clock.GetUtcNow();
        var credentials = new SigningCredentials(
            new RsaSecurityKey(jwt.PrivateRsa!) { KeyId = jwt.KeyId },
            SecurityAlgorithms.RsaSha256);
        var token = new JwtSecurityToken(
            issuer: JwtConfiguration.Issuer,
            audience: JwtConfiguration.Audience,
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim("name", user.DisplayName),
                new Claim("role", user.Role),
                new Claim(JwtRegisteredClaimNames.Iat, now.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            ],
            notBefore: now.UtcDateTime,
            expires: now.AddMinutes(15).UtcDateTime,
            signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
