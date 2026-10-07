using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Commerce.Api.Models;
using Microsoft.IdentityModel.Tokens;

namespace Commerce.Api.Services;

public sealed class TokenService(IConfiguration configuration, SymmetricSecurityKey signingKey)
{
    public LoginResult Create(AppUser user)
    {
        var expiry = DateTime.UtcNow.AddMinutes(configuration.GetValue("Authentication:LifetimeMinutes", 60));
        var token = new JwtSecurityToken(configuration["Authentication:Issuer"], configuration["Authentication:Audience"],
            [new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()), new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim("name", user.Name), new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())],
            notBefore: DateTime.UtcNow, expires: expiry, signingCredentials: new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256));
        return new(new JwtSecurityTokenHandler().WriteToken(token), expiry, new(user.Email, user.Name));
    }

    public static SymmetricSecurityKey GetKey(IConfiguration configuration, IHostEnvironment environment)
    {
        var configured = configuration["Authentication:SigningKey"];
        if (!string.IsNullOrEmpty(configured))
        {
            if (Encoding.UTF8.GetByteCount(configured) < 32) throw new InvalidOperationException("Authentication:SigningKey requiere al menos 32 bytes.");
            return new(Encoding.UTF8.GetBytes(configured));
        }
        if (!environment.IsDevelopment()) throw new InvalidOperationException("Configure Authentication__SigningKey fuera de Development.");
        // Solo desarrollo: no se publica una clave fija en Git. Reiniciar invalida las sesiones.
        return new(System.Security.Cryptography.RandomNumberGenerator.GetBytes(64));
    }
}
