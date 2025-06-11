using BCrypt.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using ReembolsoBAS.Data;
using ReembolsoBAS.Models;
using ReembolsoBAS.Models.Dto;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

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
                nome = usuario.Nome,
                id = usuario.Id
            });
        }
        
        // 2. USUÁRIO: Trocar a própria senha        
        [HttpPost("change-password")]
        [Authorize]  // precisa estar autenticado
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest req)
        {
            // 1) Identifica usuário pelo claim de matrícula
            var matricula = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(matricula))
                return Unauthorized();

            // 2) Busca o usuário no banco
            var usuario = await _ctx.Usuarios
                                    .FirstOrDefaultAsync(u => u.Matricula == matricula);
            if (usuario == null)
                return Unauthorized();

            // 3) Verifica se a senha atual está correta
            if (!BCrypt.Net.BCrypt.Verify(req.CurrentPassword, usuario.SenhaHash))
                return BadRequest("Senha atual incorreta.");

            // 4) Gera o hash da nova senha e salva
            usuario.SenhaHash = BCrypt.Net.BCrypt.HashPassword(req.NewPassword, workFactor: 12);
            await _ctx.SaveChangesAsync();

            // 5) Retorna 204 No Content para indicar sucesso
            return NoContent();
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
