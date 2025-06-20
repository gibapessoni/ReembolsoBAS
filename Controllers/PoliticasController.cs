using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ReembolsoBAS.Data;
using ReembolsoBAS.Models;
using ReembolsoBAS.Models.Dto;
using ReembolsoBAS.Services;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ReembolsoBAS.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PoliticasController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly FileStorageService _fileStorage;
        private readonly ConfigArquivos _configArquivos;

        public PoliticasController(AppDbContext context,
                                    FileStorageService fileStorage,
                                    IOptions<ConfigArquivos> configArquivos)
        {
            _context = context;
            _fileStorage = fileStorage;
            _configArquivos = configArquivos.Value;
        }
        [HttpPost("upload")]
        public async Task<IActionResult> UploadPolitica([FromForm] UploadPoliticaRequest req)
        {
            if (req.Arquivo is null || req.Arquivo.Length == 0)
                return BadRequest("Arquivo obrigatório.");

            var pasta = Path.Combine(Directory.GetCurrentDirectory(),
                                     _configArquivos.CaminhoPoliticas);   
            if (!Directory.Exists(pasta))
                Directory.CreateDirectory(pasta);

            var stored = $"{Guid.NewGuid():N}{Path.GetExtension(req.Arquivo.FileName)}";
            var full = Path.Combine(pasta, stored);
            await using (var fs = new FileStream(full, FileMode.Create))
                await req.Arquivo.CopyToAsync(fs);

            /* 3) Desativa TODAS as políticas vigentes em uma única query */
            await _context.PoliticasBAS
                          .Where(p => p.Vigente)
                          .ExecuteUpdateAsync(setters => setters.SetProperty(p => p.Vigente, false));           

            string proximaRevisao = (_context.PoliticasBAS.Count() + 1).ToString("00");

            var nova = new PoliticaBAS
            {
                Codigo = req.Codigo,
                Revisao = proximaRevisao,
                DataPublicacao = DateTime.UtcNow,
                CaminhoArquivo = stored,
                Vigente = true
            };

            _context.PoliticasBAS.Add(nova);
            await _context.SaveChangesAsync();

            return Ok(nova);
        }


        [HttpGet("todas")]
        public async Task<IActionResult> GetTodasPoliticas()
        {
            var lista = await _context.PoliticasBAS
                                      .AsNoTracking()
                                      .OrderByDescending(p => p.DataPublicacao)
                                      .ToListAsync();
            return Ok(lista);
        }
        [HttpGet("vigente")]
        public async Task<IActionResult> GetPoliticasVigentes()
        {
            var lista = await _context.PoliticasBAS
                                      .AsNoTracking()
                                      .Where(p => p.Vigente)
                                      .OrderByDescending(p => p.DataPublicacao)
                                      .ToListAsync();

            return Ok(lista);   
        }


        [HttpGet("ativas")]
        public async Task<IActionResult> GetPoliticasAtivas()
        {
            var lista = await _context.PoliticasBAS
                                      .Where(p => p.Vigente)
                                      .ToListAsync();
            return Ok(lista);
        }


        [HttpGet("download/{id:int}")]
        public async Task<IActionResult> DownloadPolitica(int id)
        {
            var politica = await _context.PoliticasBAS.FindAsync(id);
            if (politica is null) return NotFound();

            var pasta = Path.Combine(
                Directory.GetCurrentDirectory(),
                _configArquivos.CaminhoPoliticas);          // **mesma pasta**

            var caminho = Path.Combine(pasta, politica.CaminhoArquivo);
            if (!System.IO.File.Exists(caminho))
                return NotFound("Arquivo não encontrado no servidor.");

            var mime = Path.GetExtension(caminho).Equals(".pdf", StringComparison.OrdinalIgnoreCase)
                       ? "application/pdf"
                       : "application/octet-stream";        // png, jpg, etc.

            var bytes = await System.IO.File.ReadAllBytesAsync(caminho);
            return File(bytes, mime, Path.GetFileName(caminho));
        }

        [HttpPost("ativar/{Id:int}")]
        public async Task<IActionResult> AtivarPolitica(int Id)
        {
            var todas = await _context.PoliticasBAS.ToListAsync();
            foreach (var p in todas) p.Vigente = false;

            var pAtiva = todas.FirstOrDefault(x => x.Id == Id);
            if (pAtiva == null) return NotFound();

            pAtiva.Vigente = true;
            await _context.SaveChangesAsync();
            return Ok(pAtiva);
        }

        [HttpGet("pagina")]
        public async Task<IActionResult> GetConteudoPoliticaVigente()
        {
            var p = await _context.PoliticasBAS.FirstOrDefaultAsync(x => x.Vigente);
            if (p == null) return NotFound("Nenhuma política vigente encontrada.");

            var pasta = Path.Combine(Directory.GetCurrentDirectory(), _configArquivos.CaminhoPoliticas);
            var htmlPath = Path.Combine(pasta, Path.ChangeExtension(p.CaminhoArquivo, ".html"));

            if (!System.IO.File.Exists(htmlPath))
                return NotFound("Conteúdo formatado da política não encontrado.");

            var textoHtml = await System.IO.File.ReadAllTextAsync(htmlPath);
            return Content(textoHtml, "text/html");
        }
    }
}