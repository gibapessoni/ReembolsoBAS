using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReembolsoBAS.Data;
using ReembolsoBAS.Models;
using BCrypt.Net; 

namespace ReembolsoBAS.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EmpregadosController : ControllerBase
    {
        private readonly AppDbContext _ctx;

        public EmpregadosController(AppDbContext ctx)
        {
            _ctx = ctx;
        }

        // 1. Retorna todos os empregados (somente RH e Admin)
        [HttpGet]
        [Authorize(Roles = "rh,admin,empregado")]
        public async Task<IActionResult> GetAll()
        {
            var lista = await _ctx.Empregados
                                  .AsNoTracking()
                                  .ToListAsync();
            return Ok(lista);
        }

        // 2. Cria um novo empregado (somente RH ou Admin)
        [HttpPost]
        [Authorize(Roles = "rh,admin")]
        public async Task<IActionResult> Create([FromBody] Empregado emp)
        {
            // 2.1) Verifica se já existe outro Empregado com essa matrícula
            if (await _ctx.Empregados.AnyAsync(e => e.Matricula == emp.Matricula))
                return Conflict($"Já existe um empregado com matrícula '{emp.Matricula}'.");

            // 2.2) Cria o Empregado primeiro
            _ctx.Empregados.Add(emp);
            await _ctx.SaveChangesAsync();  // emp.Id é populado aqui

            // 2.3) Gera hash de senha padrão
            var senhaPadrao = "Senha123!";
            var hash = BCrypt.Net.BCrypt.HashPassword(senhaPadrao, workFactor: 12);

            // 2.4) Cria o Usuário associado ao Empregado
            var novoUsuario = new Usuario
            {
                EmpregadoId = emp.Id,                      
                Matricula = emp.Matricula,                 
                Nome = emp.Nome,
                Email = $"{emp.Matricula}@bas.com",
                SenhaHash = hash,
                Perfil = "empregado"
            };

            _ctx.Usuarios.Add(novoUsuario);
            await _ctx.SaveChangesAsync();

            // 2.5) Retorna CreatedAtAction apontando para GetById
            return CreatedAtAction(nameof(GetById), new { id = emp.Id }, emp);
        }


        // 3. Busca um empregado por Id (somente RH e Admin)
        [HttpGet("{id:int}")]
        [Authorize(Roles = "rh,admin,empregado")]
        public async Task<IActionResult> GetById(int id)
        {
            var emp = await _ctx.Empregados.FindAsync(id);
            if (emp == null)
                return NotFound();
            return Ok(emp);
        }

        // 4. Atualiza um empregado existente (somente RH ou Admin)
        [HttpPut("{id:int}")]
        [Authorize(Roles = "rh,admin")]
        public async Task<IActionResult> Update(int id, Empregado emp)
        {
            if (id != emp.Id)
                return BadRequest();

            _ctx.Entry(emp).State = EntityState.Modified;
            await _ctx.SaveChangesAsync();
            return NoContent();
        }

        // 5. Exclui um empregado (somente Admin)
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "admin")] //se quiser colocar RH também, pode ser "rh,admin"
        public async Task<IActionResult> Delete(int id)
        {
            var emp = await _ctx.Empregados.FindAsync(id);
            if (emp == null)
                return NotFound();

            // 5.1) Remover primeiro o usuário associado (se existir)
            var usuario = await _ctx.Usuarios
                                    .FirstOrDefaultAsync(u => u.Matricula == emp.Matricula);
            if (usuario != null)
                _ctx.Usuarios.Remove(usuario);

            // 5.2) Remover o empregado
            _ctx.Empregados.Remove(emp);

            await _ctx.SaveChangesAsync();
            return NoContent();
        }

        // 6. Upload em lote de empregados via Excel (somente RH ou Admin)
        [HttpPost("upload")]
        [Authorize(Roles = "rh,admin")]
        public async Task<IActionResult> UploadListaEmpregados(IFormFile arquivo)
        {
            if (arquivo == null || arquivo.Length == 0)
                return BadRequest("Arquivo obrigatório para upload.");

            using var ms = new MemoryStream();
            await arquivo.CopyToAsync(ms);
            ms.Seek(0, SeekOrigin.Begin);

            using var workbook = new XLWorkbook(ms);
            var ws = workbook.Worksheets.First();
            var linhas = ws.RowsUsed().Skip(1);

            var novosEmpregados = new List<Empregado>();
            var novosUsuarios = new List<Usuario>();

            foreach (var row in linhas)
            {
                string matricula = row.Cell(1).GetString().Trim();
                string nome = row.Cell(2).GetString().Trim();
                string diretoria = row.Cell(3).GetString().Trim();
                string superint = row.Cell(4).GetString().Trim();
                string cargo = row.Cell(5).GetString().Trim();
                bool ativo = row.Cell(6).GetValue<bool>();
                decimal valorMax = row.Cell(7).GetValue<decimal>();

                // Ignora linhas em branco ou duplicadas
                if (string.IsNullOrEmpty(matricula))
                    continue;

                bool existeEmp = await _ctx.Empregados.AnyAsync(e => e.Matricula == matricula);
                bool existeUsu = await _ctx.Usuarios.AnyAsync(u => u.Matricula == matricula);

                if (existeEmp || existeUsu)
                    continue;

                var emp = new Empregado
                {
                    Matricula = matricula,
                    Nome = nome,
                    Diretoria = diretoria,
                    Superintendencia = superint,
                    Cargo = cargo,
                    Ativo = ativo,
                    ValorMaximoMensal = valorMax
                };
                novosEmpregados.Add(emp);

                // cria usuário padrão (senha “Senha123!”)
                string senhaPadrao = "Senha123!"; // senha padrão para novos usuários
                string hashSenha = BCrypt.Net.BCrypt.HashPassword(senhaPadrao, workFactor: 12);

                var usu = new Usuario
                {
                    Nome = nome,
                    Email = $"{matricula}@empresa.com",
                    SenhaHash = hashSenha,
                    Perfil = "empregado",
                    Matricula = matricula
                };
                novosUsuarios.Add(usu);
            }

            if (novosEmpregados.Count > 0)
            {
                // Adiciona ao contexto em lote
                _ctx.Empregados.AddRange(novosEmpregados);
                _ctx.Usuarios.AddRange(novosUsuarios);
                await _ctx.SaveChangesAsync();
            }

            return Ok(new { totalImportados = novosEmpregados.Count });
        }
    }
}
