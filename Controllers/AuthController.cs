using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using BCrypt.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using ReembolsoBAS.Data;
using ReembolsoBAS.Models;

namespace ReembolsoBAS.Controllers
{
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
            // 1) Busca o usuário pelo e-mail
            var usuario = await _ctx.Usuarios
                                    .FirstOrDefaultAsync(u => u.Email == request.Email);

            // INSIRA AQUI OS CONSOLE.WriteLine PARA DEBUG:
            if (usuario == null)
            {
                Console.WriteLine($"[DEBUG] Usuário com e-mail '{request.Email}' NÃO encontrado.");
                return Unauthorized("Usuário ou senha inválidos.");
            }

            Console.WriteLine($"[DEBUG] Usuário encontrado: Id={usuario.Id}, Email={usuario.Email}");
            Console.WriteLine($"[DEBUG] SenhaHash do banco (Length = {usuario.SenhaHash.Length}): '{usuario.SenhaHash}'");

            // 2) Verifica a senha (hash)
            bool verifica = BCrypt.Net.BCrypt.Verify(request.Senha, usuario.SenhaHash);
            Console.WriteLine($"[DEBUG] BCrypt.Verify(\"{request.Senha}\", hash) retornou = {verifica}");

            if (!verifica)
                return Unauthorized("Usuário ou senha inválidos.");

            // 3) Gera o token JWT
            var token = GenerateJwtToken(usuario);
            return Ok(new
            {
                token,
                perfil = usuario.Perfil,
                matricula = usuario.Matricula,
                nome = usuario.Nome
            });
        }


        private string GenerateJwtToken(Usuario usuario)
        {
            // Chave secreta (deve estar em appsettings.json ou em variável de ambiente)
            var key = _cfg["Jwt:Key"]
                      ?? throw new InvalidOperationException("JWT key não configurada.");

            var keyBytes = Encoding.ASCII.GetBytes(key);
            var creds = new SigningCredentials(
                new SymmetricSecurityKey(keyBytes),
                SecurityAlgorithms.HmacSha256);

            // Claims que serão inseridos no payload do token
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.Matricula),
                new Claim(ClaimTypes.Email, usuario.Email),
                new Claim(ClaimTypes.Name, usuario.Nome),
                new Claim(ClaimTypes.Role, usuario.Perfil)
            };

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.UtcNow.AddHours(8),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
