using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReembolsoBAS.Data;
using ReembolsoBAS.Models;
using ReembolsoBAS.Services;
using System;
using System.Linq;
using System.Security.Claims;
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

        //------------------------------------------------------------------
        // 1. Empregado – lista “Meus Reembolsos”
        //------------------------------------------------------------------
        [HttpGet("meus")]
        [Authorize(Roles = "empregado")]
        public async Task<IActionResult> GetMeus()
        {
            var matricula = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (matricula is null) return Unauthorized();

            var lista = await _ctx.Reembolsos
                                  .Where(r => r.MatriculaEmpregado == matricula)
                                  .ToListAsync();
            return Ok(lista);
        }

        //------------------------------------------------------------------
        // 2. Empregado – nova solicitação
        //------------------------------------------------------------------
        [HttpPost("solicitar")]
        [Authorize(Roles = "empregado")]
        public async Task<IActionResult> SolicitarReembolso([FromForm] ReembolsoRequest req)
        {
            // (a) Prazo: até dia 5 do mês seguinte
            var fimMes = new DateTime(req.Periodo.Year, req.Periodo.Month, 1)
                         .AddMonths(1).AddDays(-1);
            if (DateTime.Today > fimMes.AddDays(5))
                return BadRequest("Período fora do prazo para solicitação.");

            // (b) Empregado existe?
            var emp = await _ctx.Empregados
                                .FirstOrDefaultAsync(e => e.Matricula == req.Matricula);
            if (emp is null) return BadRequest("Matrícula não encontrada.");

            // (c) Persiste
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

        //------------------------------------------------------------------
        // 3. RH – pendentes
        //------------------------------------------------------------------
        [HttpGet("pendentes-rh")]
        [Authorize(Roles = "rh,gerente_rh")]
        public async Task<IActionResult> PendentesRH()
        {
            var lista = await _ctx.Reembolsos
                                  .Where(r => r.Status == StatusReembolso.Pendente
                                           || r.Status == StatusReembolso.DevolvidoRH)
                                  .ToListAsync();
            return Ok(lista);
        }

        //------------------------------------------------------------------
        // 4. Workflow RH / Ger. RH
        //------------------------------------------------------------------
        [HttpPost("aprovar/{id:int}")]
        [Authorize(Roles = "gerente_rh")]
        public async Task<IActionResult> Aprovar(int id)
        {
            await _service.AprovarReembolso(id);
            return NoContent();
        }

        [HttpPost("reprovar/{id:int}")]
        [Authorize(Roles = "rh,gerente_rh")]
        public async Task<IActionResult> Reprovar(int id, [FromBody] string motivo)
        {
            await _service.ReprovarReembolso(id, motivo);
            return NoContent();
        }

        [HttpPost("devolver/{id:int}")]
        [Authorize(Roles = "gerente_rh")]
        public async Task<IActionResult> Devolver(int id, [FromBody] string motivo)
        {
            await _service.DevolverParaCorrecao(id, motivo);
            return NoContent();
        }
    }
}
