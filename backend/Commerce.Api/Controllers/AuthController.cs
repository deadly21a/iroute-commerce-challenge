using Commerce.Api.Data;
using Commerce.Api.Models;
using Commerce.Api.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Commerce.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(CommerceRepository repository, TokenService tokens) : ControllerBase
{
    // Hash de referencia para no omitir el coste de verificación cuando el usuario no existe.
    private static readonly AppUser DummyUser = new(0, "", "", "");
    private static readonly PasswordHasher<AppUser> Hasher = new();
    private static readonly string DummyHash = Hasher.HashPassword(DummyUser, "unusable-dummy-password");

    [HttpPost("login")]
    [EnableRateLimiting("login")]
    public async Task<ActionResult<LoginResult>> Login(LoginRequest request, CancellationToken ct)
    {
        var user = await repository.FindUserAsync(request.Email, ct);
        var result = Hasher.VerifyHashedPassword(user ?? DummyUser, user?.PasswordHash ?? DummyHash, request.Password);
        if (user is null || result == PasswordVerificationResult.Failed)
            return Problem(statusCode: 401, title: "Credenciales incorrectas", detail: "Verifica tu correo y contraseña.");
        return Ok(tokens.Create(user));
    }
}
