
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReembolsoBAS.Data;
using ReembolsoBAS.Models;
using ReembolsoBAS.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ReembolsoBAS.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "admin,rh")]
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
            var caminho = await _fileStorage.SaveFiles(new FormFileCollection { arquivo });

            var novaPolitica = new PoliticaBAS
            {
                Codigo = "PG.DAF.014/2020",
                Revisao = "03",
                DataPublicacao = DateTime.Now,
                CaminhoArquivo = caminho,
                Vigente = true
            };

            _context.PoliticasBAS.Add(novaPolitica);
            await _context.SaveChangesAsync();

            return Ok(novaPolitica);
        }

        [HttpGet("ativas")]
        public async Task<IActionResult> GetPoliticasAtivas()
        {
            var politicas = await _context.PoliticasBAS
                .Where(p => p.Vigente)
                .ToListAsync();

            return Ok(politicas);
        }
    }
}

