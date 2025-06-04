using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReembolsoBAS.Data;
using ReembolsoBAS.Models;
using ReembolsoBAS.Services;
using ClosedXML.Excel;
using System.Security.Claims;
using System.IO;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ReembolsoBAS.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReembolsosController : ControllerBase
    {
        private readonly AppDbContext _ctx;
        private readonly FileStorageService _fileStorage;
        private readonly ReembolsoService _service;

        public ReembolsosController(
            AppDbContext ctx,
            FileStorageService fileStorage,
            ReembolsoService service)
        {
            _ctx = ctx;
            _fileStorage = fileStorage;
            _service = service;
        }

        // 1. EMPREGADO: “Meus Reembolsos”
        [HttpGet("meus")]
        public async Task<IActionResult> GetMeus()
        {
            // Se você não tiver mais o ClaimTypes.NameIdentifier,
            // talvez queira obter a “matrícula” de outra forma.
            var matricula = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(matricula))
                return BadRequest("Não foi possível identificar o usuário.");

            var lista = await _ctx.Reembolsos
                                  .Where(r => r.MatriculaEmpregado == matricula)
                                  .OrderByDescending(r => r.Periodo)
                                  .ToListAsync();
            return Ok(lista);
        }

        // 2. EMPREGADO: Nova Solicitação de Reembolso
        [HttpPost("solicitar")]
        public async Task<IActionResult> SolicitarReembolso([FromForm] ReembolsoRequest req)
        {
            var fimMes = new DateTime(req.Periodo.Year, req.Periodo.Month, 1)
                         .AddMonths(1).AddDays(-1);
            if (DateTime.Today > fimMes.AddDays(5))
                return BadRequest("Período fora do prazo para solicitação.");

            var emp = await _ctx.Empregados
                                .FirstOrDefaultAsync(e => e.Matricula == req.Matricula);
            if (emp == null)
                return BadRequest("Matrícula não encontrada.");

            var reembolso = new Reembolso
            {
                MatriculaEmpregado = req.Matricula,
                Periodo = req.Periodo,
                ValorSolicitado = req.ValorSolicitado,
                Status = StatusReembolso.Pendente,
                CaminhoDocumentos = await _fileStorage.SaveFiles(req.Documentos),
                Empregado = emp
            };

            _ctx.Reembolsos.Add(reembolso);
            await _ctx.SaveChangesAsync();
            return Ok(reembolso);
        }

        // 3. RH: Listar Reembolsos Pendentes
        [HttpGet("pendentes-rh")]
        public async Task<IActionResult> PendentesRH()
        {
            var lista = await _ctx.Reembolsos
                                  .Where(r => r.Status == StatusReembolso.Pendente
                                           || r.Status == StatusReembolso.DevolvidoRH)
                                  .OrderBy(r => r.DataEnvio)
                                  .ToListAsync();
            return Ok(lista);
        }

        // 4. RH: Validar um Reembolso (marca como “ValidadoRH”)
        [HttpPost("validar/{id:int}")]
        public async Task<IActionResult> Validar(int id)
        {
            await _service.ValidarReembolso(id);
            return NoContent();
        }

        // 5. Gerente RH: Aprovar Reembolso individualmente
        [HttpPost("aprovar/{id:int}")]
        public async Task<IActionResult> Aprovar(int id)
        {
            await _service.AprovarReembolso(id);
            return NoContent();
        }

        // 6. RH ou Gerente RH: Reprovar Reembolso
        [HttpPost("reprovar/{id:int}")]
        public async Task<IActionResult> Reprovar(int id, [FromBody] string motivo)
        {
            await _service.ReprovarReembolso(id, motivo);
            return NoContent();
        }

        // 7. Gerente RH: Devolver para Correção
        [HttpPost("devolver/{id:int}")]
        public async Task<IActionResult> Devolver(int id, [FromBody] string motivo)
        {
            await _service.DevolverParaCorrecao(id, motivo);
            return NoContent();
        }

        // 8. Gerente RH: Aprovar Em Lote
        public class BatchApproveRequest
        {
            public List<int> ReembolsoIds { get; set; } = new();
        }

        [HttpPost("aprovar-em-lote")]
        public async Task<IActionResult> AprovarEmLote([FromBody] BatchApproveRequest req)
        {
            foreach (var id in req.ReembolsoIds)
            {
                await _service.AprovarReembolso(id);
            }
            return NoContent();
        }

        // 9. Relatório BAS
        public class RelatorioFilter
        {
            public DateTime Competencia { get; set; }
            public bool ApenasAprovados { get; set; } = false;
        }

        [HttpPost("relatorio")]
        public async Task<IActionResult> GetRelatorio([FromBody] RelatorioFilter filtro)
        {
            var query = _ctx.Reembolsos
                            .Include(r => r.Empregado)
                            .Where(r => r.Periodo.Year == filtro.Competencia.Year
                                     && r.Periodo.Month == filtro.Competencia.Month);

            if (filtro.ApenasAprovados)
                query = query.Where(r => r.Status == StatusReembolso.Aprovado);

            var lista = await query
                .Select(r => new
                {
                    r.Empregado.Matricula,
                    Nome = r.Empregado.Nome,
                    Diretoria = r.Empregado.Diretoria,
                    Cargo = r.Empregado.Cargo,
                    ValorMaximo = r.Empregado.ValorMaximoMensal,
                    ValorSolicitado = r.ValorSolicitado,
                    ValorReembolsado = r.ValorReembolsado,
                    r.Status,
                    DataEnvio = r.DataEnvio
                })
                .ToListAsync();

            return Ok(lista);
        }

        // 10. Relatório em Excel
        [HttpPost("relatorio/excel")]
        public async Task<IActionResult> ExportarExcel([FromBody] RelatorioFilter filtro)
        {
            var query = _ctx.Reembolsos
                            .Include(r => r.Empregado)
                            .Where(r => r.Periodo.Year == filtro.Competencia.Year
                                     && r.Periodo.Month == filtro.Competencia.Month);

            if (filtro.ApenasAprovados)
                query = query.Where(r => r.Status == StatusReembolso.Aprovado);

            var lista = await query
                .Select(r => new
                {
                    r.Empregado.Matricula,
                    Nome = r.Empregado.Nome,
                    Diretoria = r.Empregado.Diretoria,
                    Cargo = r.Empregado.Cargo,
                    ValorMaximo = r.Empregado.ValorMaximoMensal,
                    ValorSolicitado = r.ValorSolicitado,
                    ValorReembolsado = r.ValorReembolsado,
                    r.Status,
                    DataEnvio = r.DataEnvio
                })
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Relatorio");

            ws.Cell(1, 1).Value = "Matrícula";
            ws.Cell(1, 2).Value = "Nome";
            ws.Cell(1, 3).Value = "Diretoria";
            ws.Cell(1, 4).Value = "Cargo";
            ws.Cell(1, 5).Value = "Valor Máximo Mês";
            ws.Cell(1, 6).Value = "Valor Solicitado";
            ws.Cell(1, 7).Value = "Valor Reembolsado";
            ws.Cell(1, 8).Value = "Status";
            ws.Cell(1, 9).Value = "Data Envio";

            for (int i = 0; i < lista.Count; i++)
            {
                var row = i + 2;
                ws.Cell(row, 1).Value = lista[i].Matricula;
                ws.Cell(row, 2).Value = lista[i].Nome;
                ws.Cell(row, 3).Value = lista[i].Diretoria;
                ws.Cell(row, 4).Value = lista[i].Cargo;
                ws.Cell(row, 5).Value = lista[i].ValorMaximo;
                ws.Cell(row, 6).Value = lista[i].ValorSolicitado;
                ws.Cell(row, 7).Value = lista[i].ValorReembolsado;
                ws.Cell(row, 8).Value = lista[i].Status;
                ws.Cell(row, 9).Value = lista[i].DataEnvio.ToString("dd/MM/yyyy");
            }

            ws.Columns().AdjustToContents();

            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            ms.Seek(0, SeekOrigin.Begin);

            var fileName = $"Relatorio_{filtro.Competencia:yyyy_MM}.xlsx";
            return File(ms.ToArray(),
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        fileName);
        }
    }
}
