﻿using ClosedXML.Excel;
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
using Microsoft.Extensions.Configuration;

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

        // 1. Colaborador: Meus Reembolsos (ou um específico)
        [HttpGet("meus")]
        [Authorize(Roles = "colaborador,admin")]
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
                        .ThenInclude(l => l.Documentos)       
                    .FirstOrDefaultAsync(r =>
                           r.MatriculaEmpregado == matricula &&
                           r.Id == id.Value);

                return item is null
                    ? NotFound($"Nenhum reembolso encontrado com Id = {id}.")
                    : Ok(item);
            }

            var lista = await _ctx.Reembolsos
                .Include(r => r.Empregado)
                .Include(r => r.Lancamentos)
                    .ThenInclude(l => l.Documentos)            
                .Where(r => r.MatriculaEmpregado == matricula)
                .OrderByDescending(r => r.Periodo)
                .ToListAsync();

            return Ok(lista);
        }

        [HttpPost("solicitar")]
        [Authorize(Roles = "colaborador,admin,diretor-presidente")]
        public async Task<IActionResult> SolicitarReembolso(
        [FromForm] ReembolsoCreateRequest req)           // ← novo DTO
        {
            /* ── 1) Período ───────────────────────────────────────────────── */
            if (!DateTime.TryParseExact(req.Periodo + "-01", "yyyy-MM-dd",
                                        CultureInfo.InvariantCulture,
                                        DateTimeStyles.None, out var periodoDt))
                return BadRequest("Período inválido (YYYY-MM).");

            var prazo = new DateTime(periodoDt.Year, periodoDt.Month, 1)
                           .AddMonths(1).AddDays(4);
            if (DateTime.Today > prazo)
                return BadRequest("Fora do prazo para solicitação.");

            /* ── 2) Empregado ─────────────────────────────────────────────── */
            var emp = await _ctx.Empregados
                                .FirstOrDefaultAsync(e => e.Matricula == req.Matricula);
            if (emp is null) return BadRequest("Matrícula não encontrada.");

            /* ── 3) Monta lançamentos ─────────────────────────────────────── */
            decimal total = 0;
            var lancs = new List<ReembolsoLancamento>(req.Lancamentos.Count);

            foreach (var dto in req.Lancamentos.Select((v, i) => (v, i)))
            {
                var (lancDto, idx) = dto;

                if (lancDto.Arquivos is null || lancDto.Arquivos.Count == 0)
                    return BadRequest($"Lançamento {idx + 1}: anexe pelo menos 1 arquivo.");

                var lanc = new ReembolsoLancamento
                {
                    Beneficiario = lancDto.Beneficiario,
                    GrauParentesco = lancDto.GrauParentesco,
                    DataNascimento = lancDto.DataNascimento,
                    ValorPago = lancDto.ValorPago,
                    ValorRestituir = lancDto.ValorPago * 0.5m,
                    TipoSolicitacao = lancDto.TipoSolicitacao
                };

                foreach (var file in lancDto.Arquivos)
                {
                    var (stored, ctype) = await _fileStorage.SaveFile(file);
                    lanc.Documentos.Add(new ReembolsoDocumento
                    {
                        NomeFisico = stored,
                        NomeOriginal = file.FileName,
                        ContentType = ctype
                    });
                }

                total += lancDto.ValorPago;
                lancs.Add(lanc);
            }

            /* ── 4) Persiste cabeçalho + lançamentos ──────────────────────── */
            var reembolso = new Reembolso
            {
                MatriculaEmpregado = emp.Matricula,
                Periodo = periodoDt,
                ValorSolicitado = total,
                Status = StatusReembolso.Pendente,
                Empregado = emp,
                Lancamentos = lancs
            };

            _ctx.Reembolsos.Add(reembolso);
            await _ctx.SaveChangesAsync();

            return CreatedAtAction(nameof(GetMeus),
                                   new { id = reembolso.Id },
                                   reembolso);
        }



        [HttpPut("{id:int}")]
        [Authorize(Roles = "colaborador,admin")]
        public async Task<IActionResult> EditarReembolso(
        int id,
        [FromForm] ReembolsoEditRequest req)
        {
            var reembolso = await _ctx.Reembolsos
                                      .Include(r => r.Lancamentos)
                                      .ThenInclude(l => l.Documentos)
                                      .FirstOrDefaultAsync(r => r.Id == id);
            if (reembolso == null) return NotFound();

            var matricula = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (reembolso.MatriculaEmpregado != matricula && !User.IsInRole("admin"))
                return Forbid();

            /* ── valida período ───────────────────────────────────────────── */
            if (!DateTime.TryParseExact(req.Periodo + "-01", "yyyy-MM-dd",
                                        CultureInfo.InvariantCulture,
                                        DateTimeStyles.None, out var periodoDt))
                return BadRequest("Período inválido (YYYY-MM).");

            var prazo = new DateTime(periodoDt.Year, periodoDt.Month, 1)
                           .AddMonths(1).AddDays(4);
            if (DateTime.Today > prazo && !User.IsInRole("admin"))
                return BadRequest("Período fora do prazo para alteração.");

            /* ── 1. lançamentos presentes no banco ───────────────────────── */
            var existentes = reembolso.Lancamentos.ToDictionary(l => l.Id);

            /* ── 2. percorre requisição ──────────────────────────────────── */
            reembolso.Lancamentos.Clear();   // repovoaremos conforme req
            decimal total = 0;

            foreach (var dto in req.Lancamentos)
            {
                ReembolsoLancamento lanc;

                if (dto.Id is not null && existentes.TryGetValue(dto.Id.Value, out lanc!))
                {
                    /* Atualizar existente */
                    existentes.Remove(dto.Id.Value);  // sobra → será deletado
                }
                else
                {
                    /* Criar novo */
                    lanc = new ReembolsoLancamento();
                    _ctx.ReembolsoLancamentos.Add(lanc);
                }

                /* Copia dados do DTO para o modelo */
                lanc.Beneficiario = dto.Beneficiario;
                lanc.GrauParentesco = dto.GrauParentesco;
                lanc.DataNascimento = dto.DataNascimento;
                lanc.ValorPago = dto.ValorPago;
                lanc.ValorRestituir = dto.ValorPago * 0.5m;
                lanc.TipoSolicitacao = dto.TipoSolicitacao;

                /* anexos adicionais (opcionais) */
                if (dto.NovosArquivos?.Any() == true)
                {
                    foreach (var file in dto.NovosArquivos)
                    {
                        var (stored, ctype) = await _fileStorage.SaveFile(file);
                        lanc.Documentos.Add(new ReembolsoDocumento
                        {
                            NomeFisico = stored,
                            NomeOriginal = file.FileName,
                            ContentType = ctype
                        });
                    }
                }

                total += dto.ValorPago;
                reembolso.Lancamentos.Add(lanc);
            }

            /* ── 3. qualquer lançamento que sobrou em 'existentes' foi removido na requisição → delete ── */
            if (existentes.Any())
                _ctx.ReembolsoLancamentos.RemoveRange(existentes.Values);

            /* ── 4. cabeçalho do reembolso ────────────────────────────────── */
            reembolso.Periodo = periodoDt;
            reembolso.ValorSolicitado = total;

            await _ctx.SaveChangesAsync();
            return Ok(reembolso);
        }


        // 2.2 colaborador: Excluir uma Solicitação de Reembolso
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "colaborador,admin,diretor-presidente")]
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
        // GET: api/Reembolsos/todos
        [HttpGet("todos")]
        [Authorize(Roles = "rh,gerente_rh,admin")]
        public async Task<IActionResult> TodosReembolsos()
        {
            var lista = await _ctx.Reembolsos
                                  .Include(r => r.Empregado)
                                  .Include(r => r.Lancamentos)
                                  .OrderBy(r => r.DataEnvio)
                                  .Select(r => new ReembolsoDto(
                                      r.Id,
                                      r.NumeroRegistro,
                                      r.Empregado.Nome,

                                      // Data de nascimento representativa (1º lançamento)
                                      r.Lancamentos
                                       .OrderBy(l => l.Id)
                                       .Select(l => l.DataNascimento)
                                       .FirstOrDefault(),

                                      r.Periodo,

                                      // Tipo de solicitação do 1º lançamento
                                      r.Lancamentos
                                       .OrderBy(l => l.Id)
                                       .Select(l => l.TipoSolicitacao)
                                       .FirstOrDefault(),

                                      r.Status,
                                      r.ValorSolicitado,
                                      r.ValorReembolsado))
                                  .ToListAsync();

            return Ok(lista);
        }

        // 4. RH: Validar um Reembolso (marca como “ValidadoRH”)
        [HttpPost("validar/{id:int}")]
        [Authorize(Roles = "rh,gerente_rh,admin,Diretor-Presidente")]
        public async Task<IActionResult> Validar(int id)
        {
            await _service.ValidarReembolso(id);
            return NoContent();
        }

        // 5. Gerente RH: Aprovar Reembolso individualmente
        [HttpPost("aprovar/{id:int}")]
        [Authorize(Roles = "gerente_rh,admin,Diretor-Presidente")]
        public async Task<IActionResult> Aprovar(int id)
        {
            await _service.AprovarReembolso(id);
            return NoContent();
        }

        // 6. RH ou Gerente RH: Reprovar Reembolso
        [HttpPost("reprovar/{id:int}")]
        [Authorize(Roles = "rh,gerente_rh,admin, Diretor-Presidente")]
        public async Task<IActionResult> Reprovar(int id, [FromBody] string motivo)
        {
            await _service.ReprovarReembolso(id, motivo);
            return NoContent();
        }

        // 7. Gerente RH: Devolver para Correção
        [HttpPost("devolver/{id:int}")]
        [Authorize(Roles = "gerente_rh,admin,Diretor-Presidente ")]
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

        // GET /api/Reembolsos/relatorio?ano=2025&mes=6&apenasAprovados=true
        [HttpGet("relatorio")]
        [Authorize(Roles = "rh,gerente_rh,admin,colaborador")]
        public async Task<IActionResult> GetRelatorio(
            [FromQuery] int ano,                     // obrigatório
            [FromQuery] int mes,                     // obrigatório (1-12)
            [FromQuery] bool apenasAprovados = false // opcional, default = false
        )
        {
            if (mes is < 1 or > 12)                 // validação simples
                return BadRequest("O mês deve estar entre 1 e 12.");

            var query = _ctx.Reembolsos
                            .Include(r => r.Empregado)
                            .Where(r => r.Periodo.Year == ano &&
                                        r.Periodo.Month == mes);

            if (apenasAprovados)
                query = query.Where(r => r.Status == StatusReembolso.Aprovado);

            var lista = await query
                .Select(r => new
                {
                    r.Empregado.Matricula,
                    Nome = r.Empregado.Nome,
                    Diretoria = r.Empregado.Diretoria,
                    Cargo = r.Empregado.Cargo,
                    ValorMaximo = r.Empregado.ValorMaximoMensal,

                    // <- aqui estava faltando o "r."
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
        // GET api/Reembolsos/documento/123
        [HttpGet("documento/{docId:int}")]
        [Authorize(Roles = "colaborador,rh,gerente_rh,admin,diretor-presidente")]
        public async Task<IActionResult> BaixarDocumento(int docId)
        {
            var doc = await _ctx.ReembolsoDocumentos
                                .Include(d => d.Lancamento).ThenInclude(l => l.Reembolso)
                                .FirstOrDefaultAsync(d => d.Id == docId);
            if (doc is null) return NotFound();

            var matricula = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (User.IsInRole("colaborador") &&
                doc.Lancamento.Reembolso.MatriculaEmpregado != matricula)
                return Forbid();

            var stream = await _fileStorage.OpenReadAsync(doc.NomeFisico);
            if (stream is null) return NotFound("Arquivo não existe no servidor.");

            return File(stream, doc.ContentType, doc.NomeOriginal, enableRangeProcessing: true);
        }

        // DELETE api/Reembolsos/documento/123
        [HttpDelete("documento/{docId:int}")]
        [Authorize(Roles = "colaborador,rh,gerente_rh,admin")]
        public async Task<IActionResult> ExcluirDocumento(int docId)
        {
            var doc = await _ctx.ReembolsoDocumentos
                                .Include(d => d.Lancamento).ThenInclude(l => l.Reembolso)
                                .FirstOrDefaultAsync(d => d.Id == docId);
            if (doc is null) return NotFound();

            var matricula = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (User.IsInRole("colaborador") &&
                doc.Lancamento.Reembolso.MatriculaEmpregado != matricula)
                return Forbid();

            await _fileStorage.DeleteFile(doc.NomeFisico);
            _ctx.Remove(doc);
            await _ctx.SaveChangesAsync();
            return NoContent();
        }



    }
}
