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
            if (req.Arquivo == null || req.Arquivo.Length == 0)
                return BadRequest("Arquivo obrigatório.");

            // 1) Marcar tudo como não-vigente
            var antigas = await _context.PoliticasBAS.ToListAsync();
            antigas.ForEach(p => p.Vigente = false);  

            // 2) Salvar o arquivo
            var nomeArquivo = await _fileStorage.SaveFiles(
                new FormFileCollection { req.Arquivo });

            // 3) Usar o código vindo do front
            var nova = new PoliticaBAS
            {
                Codigo = req.Codigo,
                Revisao = (antigas.Count + 1).ToString("00"),
                DataPublicacao = DateTime.UtcNow,
                CaminhoArquivo = nomeArquivo,
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

        [HttpGet("ativas")]
        public async Task<IActionResult> GetPoliticasAtivas()
        {
            var lista = await _context.PoliticasBAS
                                      .Where(p => p.Vigente)
                                      .ToListAsync();
            return Ok(lista);
        }

        [HttpGet("download/{Id:int}")]
        public async Task<IActionResult> DownloadPolitica(int Id)
        {
            var p = await _context.PoliticasBAS.FindAsync(Id);
            if (p == null) return NotFound();

            var pasta = Path.Combine(Directory.GetCurrentDirectory(), _configArquivos.CaminhoPoliticas);
            var caminhoCompleto = Path.Combine(pasta, p.CaminhoArquivo);

            if (!System.IO.File.Exists(caminhoCompleto))
                return NotFound("Arquivo não encontrado no servidor.");

            var fileName = Path.GetFileName(caminhoCompleto);
            var mimeType = "application/pdf";
            var bytes = await System.IO.File.ReadAllBytesAsync(caminhoCompleto);
            return File(bytes, mimeType, fileName);
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