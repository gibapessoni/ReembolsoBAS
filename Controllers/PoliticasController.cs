using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReembolsoBAS.Data;
using ReembolsoBAS.Models;
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

        public PoliticasController(AppDbContext context, FileStorageService fileStorage)
        {
            _context = context;
            _fileStorage = fileStorage;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadPolitica(IFormFile arquivo)
        {
            if (arquivo == null || arquivo.Length == 0)
                return BadRequest("Arquivo obrigatório");

            var todas = await _context.PoliticasBAS.ToListAsync();
            foreach (var p in todas) p.Vigente = false;

            var caminho = await _fileStorage.SaveFiles(new FormFileCollection { arquivo });

            var novaPolitica = new PoliticaBAS
            {
                Codigo = "PG.DAF.014/2020",
                Revisao = (todas.Count + 1).ToString("00"),
                DataPublicacao = DateTime.UtcNow,
                CaminhoArquivo = caminho,
                Vigente = true
            };

            _context.PoliticasBAS.Add(novaPolitica);
            await _context.SaveChangesAsync();

            return Ok(novaPolitica);
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
            var p = await _context.PoliticasBAS.FindAsync(id);
            if (p == null) return NotFound();

            if (!System.IO.File.Exists(p.CaminhoArquivo))
                return NotFound("Arquivo não encontrado no servidor.");

            var fileName = Path.GetFileName(p.CaminhoArquivo);
            var mimeType = "application/pdf";
            var bytes = await System.IO.File.ReadAllBytesAsync(p.CaminhoArquivo);
            return File(bytes, mimeType, fileName);
        }

        [HttpPost("ativar/{id:int}")]
        public async Task<IActionResult> AtivarPolitica(int id)
        {
            var todas = await _context.PoliticasBAS.ToListAsync();
            foreach (var p in todas) p.Vigente = false;

            var pAtiva = todas.FirstOrDefault(x => x.Id == id);
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

            var htmlPath = Path.ChangeExtension(p.CaminhoArquivo, ".html");
            if (!System.IO.File.Exists(htmlPath))
                return NotFound("Conteúdo formatado da política não encontrado.");

            var textoHtml = await System.IO.File.ReadAllTextAsync(htmlPath);
            return Content(textoHtml, "text/html");
        }
    }

}
