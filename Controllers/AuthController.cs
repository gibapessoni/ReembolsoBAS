using BCrypt.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using ReembolsoBAS.Data;
using ReembolsoBAS.Models;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace ReembolsoBAS.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _ctx;
    private readonly IConfiguration _cfg;

    public AuthController(AppDbContext ctx, IConfiguration cfg)
    {
        _ctx = ctx;
        _cfg = cfg;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var usuario = await _ctx.Usuarios
                                .FirstOrDefaultAsync(u => u.Email == request.Email);

        if (usuario == null || !BCrypt.Net.BCrypt.Verify(request.Senha, usuario.SenhaHash))
            return Unauthorized();

        var token = GenerateJwtToken(usuario);
        return Ok(new { token, perfil = usuario.Perfil });
    }

    private string GenerateJwtToken(Usuario usuario)
    {
        var key = _cfg["Jwt:Key"]
                  ?? throw new InvalidOperationException("JWT key não configurada.");

        var creds = new SigningCredentials(
                        new SymmetricSecurityKey(Encoding.ASCII.GetBytes(key)),
                        SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.Email,  usuario.Email),
            new Claim(ClaimTypes.Role,   usuario.Perfil),
            new Claim(ClaimTypes.NameIdentifier, usuario.Matricula) // 👈 nova claim
        };

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
