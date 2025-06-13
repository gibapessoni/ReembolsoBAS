using BCrypt.Net;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ReembolsoBAS.Data;
using ReembolsoBAS.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ReembolsoBAS.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EmpregadosController : ControllerBase
    {
        private readonly AppDbContext _ctx;
        private readonly ILogger<EmpregadosController> _logger;

        public EmpregadosController(AppDbContext ctx, ILogger<EmpregadosController> logger)
        {
            _ctx = ctx;
            _logger = logger;
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
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                // 2.1) Verifica se já existe outro Empregado com essa matrícula
                if (await _ctx.Empregados.AnyAsync(e => e.Matricula == emp.Matricula))
                    return Conflict($"Já existe um empregado com matrícula '{emp.Matricula}'.");

                using var transaction = await _ctx.Database.BeginTransactionAsync();

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
                    Email = $"{emp.Matricula}@reembolsoBas.com",
                    SenhaHash = hash,
                    Perfil = "empregado"
                };

                _ctx.Usuarios.Add(novoUsuario);
                await _ctx.SaveChangesAsync();

                await transaction.CommitAsync();

                // 2.5) Retorna CreatedAtAction apontando para GetById
                return CreatedAtAction(nameof(GetById), new { id = emp.Id }, emp);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar empregado.");
                return StatusCode(500, "Erro interno do servidor.");
            }
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
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (id != emp.Id)
                return BadRequest();

            try
            {
                _ctx.Entry(emp).State = EntityState.Modified;
                await _ctx.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar empregado.");
                return StatusCode(500, "Erro interno do servidor.");
            }
        }

        // 5. Exclui um empregado (somente Admin)
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var emp = await _ctx.Empregados.FindAsync(id);
                if (emp == null)
                    return NotFound();

                // 5.1) Remover primeiro o usuário associado (se existir)
                var usuario = await _ctx.Usuarios.FirstOrDefaultAsync(u => u.Matricula == emp.Matricula);
                if (usuario != null)
                    _ctx.Usuarios.Remove(usuario);

                // 5.2) Remover o empregado
                _ctx.Empregados.Remove(emp);

                await _ctx.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao excluir empregado.");
                return StatusCode(500, "Erro interno do servidor.");
            }
        }

        // 6. Upload em lote de empregados via Excel (somente RH ou Admin)
        /// <summary>Importa empregados a partir de arquivo Excel (.xlsx).</summary>
        [HttpPost("upload")]
        [Authorize(Roles = "rh,admin")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadListaEmpregados(IFormFile arquivo)
        {
            if (arquivo == null || arquivo.Length == 0)
                return BadRequest("Envie um arquivo Excel (.xlsx).");

            var erros = new List<string>();
            var novosEmpregados = new List<Empregado>();
            var hashMatriculas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // ---------- LÊ O ARQUIVO ----------
            using var ms = new MemoryStream();
            await arquivo.CopyToAsync(ms);
            ms.Position = 0;

            using var wb = new XLWorkbook(ms);
            var ws = wb.Worksheets.First();

            if (ws.Row(1).CellsUsed().Count() < 7)
                return BadRequest("Layout incorreto: mínimo 7 colunas.");

            foreach (var row in ws.RowsUsed().Skip(1))
            {
                int linha = row.RowNumber();
                var matricula = row.Cell(1).GetString().Trim().ToUpperInvariant();

                if (string.IsNullOrEmpty(matricula))
                {
                    erros.Add($"Linha {linha}: matrícula vazia.");
                    continue;
                }
                if (!hashMatriculas.Add(matricula))
                {
                    erros.Add($"Linha {linha}: matrícula '{matricula}' repetida no arquivo.");
                    continue;
                }
                if (await _ctx.Empregados.AnyAsync(e => e.Matricula == matricula) ||
                    await _ctx.Usuarios.AnyAsync(u => u.Matricula == matricula))
                {
                    erros.Add($"Linha {linha}: matrícula '{matricula}' já cadastrada.");
                    continue;
                }

                // Demais campos
                var nome = row.Cell(2).GetString().Trim();
                var diret = row.Cell(3).GetString().Trim();
                var super = row.Cell(4).GetString().Trim();
                var cargo = row.Cell(5).GetString().Trim();

                var ativo = row.Cell(6).GetString().Trim().ToLowerInvariant() switch
                {
                    "1" or "true" or "sim" => true,
                    "0" or "false" or "não" or "nao" => false,
                    _ => throw new FormatException($"Linha {linha}: valor inválido na coluna 'Ativo'.")
                };

                if (!decimal.TryParse(row.Cell(7).GetString().Trim().Replace('.', ','),
                                      NumberStyles.Any, new CultureInfo("pt-BR"),
                                      out var valorMax))
                {
                    erros.Add($"Linha {linha}: valor máximo inválido.");
                    continue;
                }

                // Adiciona empregado válido
                novosEmpregados.Add(new Empregado
                {
                    Matricula = matricula,
                    Nome = nome,
                    Diretoria = diret,
                    Superintendencia = super,
                    Cargo = cargo,
                    Ativo = ativo,
                    ValorMaximoMensal = valorMax
                });
            }

            if (erros.Any()) return BadRequest(new { erros });
            if (!novosEmpregados.Any()) return NoContent();

            // ---------- GRAVA NO BANCO ----------
            await using var trx = await _ctx.Database.BeginTransactionAsync();
            try
            {
                _ctx.Empregados.AddRange(novosEmpregados);
                await _ctx.SaveChangesAsync();          // gera IDs

                // Agora cria usuários — PERFIL = e.Cargo
                var novosUsuarios = novosEmpregados.Select(e => new Usuario
                {
                    EmpregadoId = e.Id,                 // FK
                    Matricula = e.Matricula,
                    Nome = e.Nome,
                    Email = $"{e.Matricula}@reembolsobas.com",
                    SenhaHash = BCrypt.Net.BCrypt.HashPassword("Senha123!", 12),
                    Perfil = e.Cargo               // <<< aqui
                }).ToList();

                _ctx.Usuarios.AddRange(novosUsuarios);
                await _ctx.SaveChangesAsync();
                await trx.CommitAsync();

                return Ok(new
                {
                    totalImportados = novosEmpregados.Count,
                    mensagem = "Importação concluída com sucesso."
                });
            }
            catch (DbUpdateException dbEx)
            {
                await trx.RollbackAsync();
                _logger.LogError(dbEx, "Falha de constraint no lote.");
                return Conflict(dbEx.InnerException?.Message ?? dbEx.Message);
            }
            catch (Exception ex)
            {
                await trx.RollbackAsync();
                _logger.LogError(ex, "Erro inesperado no upload.");
                return StatusCode(500, "Erro interno do servidor.");
            }
        }

    }
}