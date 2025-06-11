using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReembolsoBAS.Data;
using ReembolsoBAS.Models;
using ReembolsoBAS.Models.Dto;
using ReembolsoBAS.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;

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

        // 1. EMPREGADO: Meus Reembolsos (ou um específico)
        [HttpGet("meus")]
        [Authorize(Roles = "empregado,admin")]
        public async Task<IActionResult> GetMeus([FromQuery] int? id = null)
        {
            var matricula = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(matricula))
                return BadRequest("Não foi possível identificar o usuário.");

            if (id.HasValue)
            {
                var item = await _ctx.Reembolsos
                    .Include(r => r.Empregado)
                    .Include(r => r.Lancamentos)
                    .Where(r => r.MatriculaEmpregado == matricula && r.Id == id.Value)
                    .FirstOrDefaultAsync();
                if (item == null)
                    return NotFound($"Nenhum reembolso encontrado com Id = {id.Value}.");
                return Ok(item);
            }

            var lista = await _ctx.Reembolsos
                                  .Include(r => r.Empregado)
                                  .Include(r => r.Lancamentos)
                                  .Where(r => r.MatriculaEmpregado == matricula)
                                  .OrderByDescending(r => r.Periodo)
                                  .ToListAsync();
            return Ok(lista);
        }

        // 2. EMPREGADO: Nova Solicitação de Reembolso
        [HttpPost("solicitar")]
        [Authorize(Roles = "empregado,admin,diretor-presidente")]
        public async Task<IActionResult> SolicitarReembolso([FromForm] ReembolsoRequest req)
        {
            // 1) Validação do formato "YYYY-MM"
            if (string.IsNullOrWhiteSpace(req.Periodo)
                || req.Periodo.Length != 7
                || !DateTime.TryParseExact(
                     req.Periodo + "-01",
                     "yyyy-MM-dd",
                     CultureInfo.InvariantCulture,
                     DateTimeStyles.None,
                     out var periodoDt))
            {
                return BadRequest("Período inválido. Envie no formato YYYY-MM.");
            }

            // 2) Valida prazo (até dia 5 do mês seguinte)
            var ultimoDiaMes = new DateTime(periodoDt.Year, periodoDt.Month, 1)
                                 .AddMonths(1)
                                 .AddDays(-1);
            if (DateTime.Today > ultimoDiaMes.AddDays(5))
                return BadRequest("Período fora do prazo para solicitação.");

            // 3) Carrega o empregado
            var emp = await _ctx.Empregados
                                .FirstOrDefaultAsync(e => e.Matricula == req.Matricula);
            if (emp == null)
                return BadRequest("Matrícula não encontrada.");

            // 4) Validação dos arrays de lançamentos
            int n = req.Beneficiario.Length;
            if (n == 0
             || req.GrauParentesco.Length != n
             || req.DataPagamento.Length != n
             || req.ValorPago.Length != n)
            {
                return BadRequest("Todos os campos de lançamento devem ter o mesmo número de itens.");
            }

            // 5) Monta a lista de lançamentos
            var lancamentos = new List<ReembolsoLancamento>(n);
            for (int i = 0; i < n; i++)
            {
                lancamentos.Add(new ReembolsoLancamento
                {
                    Beneficiario = req.Beneficiario[i],
                    GrauParentesco = req.GrauParentesco[i],
                    DataPagamento = req.DataPagamento[i],
                    ValorPago = req.ValorPago[i],
                    ValorRestituir = req.ValorPago[i] * 0.5m
                });
            }

            // 6) Cria o reembolso **em PENDENTE**
            var reembolso = new Reembolso
            {
                MatriculaEmpregado = req.Matricula,
                TipoSolicitacao = req.TipoSolicitacao,
                Periodo = periodoDt,
                ValorSolicitado = req.ValorSolicitado,
                Status = StatusReembolso.Pendente,
                CaminhoDocumentos = await _fileStorage.SaveFiles(req.Documentos),
                Empregado = emp,
                Lancamentos = lancamentos
            };

            if (req.Documentos == null || req.Documentos.Count == 0)
                return BadRequest("É obrigatório enviar ao menos um documento.");

            _ctx.Reembolsos.Add(reembolso);
            await _ctx.SaveChangesAsync();
            
            return CreatedAtAction(
                nameof(GetMeus),
                new { },
                reembolso
            );
        }


        // 2.1 EMPREGADO: Editar uma Solicitação de Reembolso
        [HttpPut("{id:int}")]
        [Authorize(Roles = "empregado,admin")]
        public async Task<IActionResult> EditarReembolso(int id, [FromForm] ReembolsoRequest req)
        {
            var reembolso = await _ctx.Reembolsos
                                      .Include(r => r.Empregado)
                                      .FirstOrDefaultAsync(r => r.Id == id);
            if (reembolso == null) return NotFound();

            var matriculaLogado = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (reembolso.MatriculaEmpregado != matriculaLogado && !User.IsInRole("admin"))
                return Forbid();

            // converte o período:
            if (string.IsNullOrWhiteSpace(req.Periodo)
                || req.Periodo.Length != 7
                || !DateTime.TryParseExact(
                     req.Periodo + "-01",
                     "yyyy-MM-dd",
                     CultureInfo.InvariantCulture,
                     DateTimeStyles.None,
                     out var periodoDt))
            {
                return BadRequest("Período inválido. Envie no formato YYYY-MM.");
            }

            // valida prazo
            var ultimoDiaMes = new DateTime(periodoDt.Year, periodoDt.Month, 1)
                                 .AddMonths(1)
                                 .AddDays(-1);
            if (DateTime.Today > ultimoDiaMes.AddDays(5) && !User.IsInRole("admin"))
                return BadRequest("Período fora do prazo para alteração.");

            // aplica mudanças
            reembolso.Periodo = periodoDt;
            reembolso.ValorSolicitado = req.ValorSolicitado;
            if (req.Documentos?.Count > 0)
                reembolso.CaminhoDocumentos = await _fileStorage.SaveFiles(req.Documentos);

            _ctx.Entry(reembolso).State = EntityState.Modified;
            await _ctx.SaveChangesAsync();
            return Ok(reembolso);
        }

        // 2.2 EMPREGADO: Excluir uma Solicitação de Reembolso
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "empregado,admin,diretor-presidente")]
        public async Task<IActionResult> ExcluirReembolso(int id)
        {
            var reembolso = await _ctx.Reembolsos
                                      .FirstOrDefaultAsync(r => r.Id == id);
            if (reembolso == null)
                return NotFound();

            var matriculaLogado = User.FindFirstValue(ClaimTypes.NameIdentifier);
            // Só o criador (ou admin) pode excluir
            if (reembolso.MatriculaEmpregado != matriculaLogado && !User.IsInRole("admin"))
                return Forbid();

            // Só exclui se ainda estiver pendente ou devolvido para correção
            if (reembolso.Status != StatusReembolso.Pendente &&
                reembolso.Status != StatusReembolso.DevolvidoRH &&
                !User.IsInRole("admin"))
            {
                return BadRequest("Não é possível apagar um reembolso já validado ou aprovado.");
            }

            _ctx.Reembolsos.Remove(reembolso);
            await _ctx.SaveChangesAsync();
            return NoContent();
        }

        // 3. RH: Listar Reembolsos Pendentes
        [HttpGet("pendentes-rh")]
        [Authorize(Roles = "rh,gerente_rh,admin")]
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
        [Authorize(Roles = "rh,gerente_rh,admin,diretor-presidente")]
        public async Task<IActionResult> Validar(int id)
        {
            await _service.ValidarReembolso(id);
            return NoContent();
        }

        // 5. Gerente RH: Aprovar Reembolso individualmente
        [HttpPost("aprovar/{id:int}")]
        [Authorize(Roles = "gerente_rh,admin")]
        public async Task<IActionResult> Aprovar(int id)
        {
            await _service.AprovarReembolso(id);
            return NoContent();
        }

        // 6. RH ou Gerente RH: Reprovar Reembolso
        [HttpPost("reprovar/{id:int}")]
        [Authorize(Roles = "rh,gerente_rh,admin")]
        public async Task<IActionResult> Reprovar(int id, [FromBody] string motivo)
        {
            await _service.ReprovarReembolso(id, motivo);
            return NoContent();
        }

        // 7. Gerente RH: Devolver para Correção
        [HttpPost("devolver/{id:int}")]
        [Authorize(Roles = "gerente_rh,admin")]
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
        [Authorize(Roles = "gerente_rh,admin")]
        public async Task<IActionResult> AprovarEmLote([FromBody] BatchApproveRequest req)
        {
            foreach (var rId in req.ReembolsoIds)
            {
                await _service.AprovarReembolso(rId);
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
        [Authorize(Roles = "rh,gerente_rh,admin")]
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
        [Authorize(Roles = "rh,gerente_rh,admin")]
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
