using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace SmartSales.Sales.Infrastructure.Security;

public sealed class JwtConfiguration : IDisposable
{
    private const string Issuer = "smartsales-contacts";
    private const string Audience = "smartsales-api";
    public required RsaSecurityKey PublicKey { get; init; }

    public static JwtConfiguration Load(IConfiguration configuration)
    {
        var publicPem = configuration["Jwt:PublicKeyPem"]
            ?? throw new InvalidOperationException("Jwt:PublicKeyPem is required.");
        var keyId = configuration["Jwt:KeyId"]
            ?? throw new InvalidOperationException("Jwt:KeyId is required.");
        var publicRsa = RSA.Create();
        publicRsa.ImportFromPem(publicPem);
        if (publicRsa.KeySize < 3072)
            throw new InvalidOperationException("JWT RSA key must be at least 3072 bits.");
        return new JwtConfiguration { PublicKey = new RsaSecurityKey(publicRsa) { KeyId = keyId } };
    }

    public static void ConfigureValidation(JwtBearerOptions options, JwtConfiguration jwt)
    {
        options.MapInboundClaims = false;
        options.IncludeErrorDetails = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = Issuer,
            ValidateAudience = true,
            ValidAudience = Audience,
            ValidateLifetime = true,
            RequireExpirationTime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = jwt.PublicKey,
            ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = "name",
            RoleClaimType = "role"
        };
    }

    public void Dispose() => PublicKey.Rsa?.Dispose();
}
