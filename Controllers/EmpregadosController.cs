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

        // 1. Retorna todos os empregados
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var lista = await _ctx.Empregados
                                  .AsNoTracking()
                                  .ToListAsync();
            return Ok(lista);
        }

        // 2. Cria um novo empregado
        [HttpPost]
        public async Task<IActionResult> Create(Empregado emp)
        {
            _ctx.Empregados.Add(emp);
            await _ctx.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = emp.Id }, emp);
        }

        // 3. Busca um empregado por Id
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var emp = await _ctx.Empregados.FindAsync(id);
            if (emp == null) return NotFound();
            return Ok(emp);
        }

        // 4. Atualiza um empregado existente
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, Empregado emp)
        {
            if (id != emp.Id)
                return BadRequest();

            _ctx.Entry(emp).State = EntityState.Modified;
            await _ctx.SaveChangesAsync();
            return NoContent();
        }

        // 5. Exclui um empregado
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var emp = await _ctx.Empregados.FindAsync(id);
            if (emp == null) return NotFound();

            _ctx.Empregados.Remove(emp);
            await _ctx.SaveChangesAsync();
            return NoContent();
        }

        // 6. Upload em lote de empregados via Excel
        [HttpPost("upload")]
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

            var novos = new List<Empregado>();
            foreach (var row in linhas)
            {
                string matricula = row.Cell(1).GetString().Trim();
                string nome = row.Cell(2).GetString().Trim();
                string diretoria = row.Cell(3).GetString().Trim();
                string superintendencia = row.Cell(4).GetString().Trim();
                string cargo = row.Cell(5).GetString().Trim();
                bool ativo = row.Cell(6).GetValue<bool>();
                decimal valorMaximo = row.Cell(7).GetValue<decimal>();

                bool existe = await _ctx.Empregados
                                        .AnyAsync(e => e.Matricula == matricula);
                if (existe)
                    continue;

                novos.Add(new Empregado
                {
                    Matricula = matricula,
                    Nome = nome,
                    Diretoria = diretoria,
                    Superintendencia = superintendencia,
                    Cargo = cargo,
                    Ativo = ativo,
                    ValorMaximoMensal = valorMaximo
                });
            }

            if (novos.Count > 0)
            {
                _ctx.Empregados.AddRange(novos);
                await _ctx.SaveChangesAsync();
            }

            return Ok(new { totalImportados = novos.Count });
        }
    }

}
