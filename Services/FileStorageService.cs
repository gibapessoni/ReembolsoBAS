using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ReembolsoBAS.Services
{
    public class FileStorageService
    {
        private readonly string _uploadPath;

        public FileStorageService()
        {
            _uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");
        }

        /* ───────────────────────── 1) SALVAR ‒ 1 arquivo ─────────────────── */
        public async Task<(string stored, string contentType)> SaveFile(IFormFile file)
        {
            if (!Directory.Exists(_uploadPath))
                Directory.CreateDirectory(_uploadPath);

            var stored = $"{Guid.NewGuid():N}{Path.GetExtension(file.FileName)}";
            var full = Path.Combine(_uploadPath, stored);

            await using var fs = new FileStream(full, FileMode.Create);
            await file.CopyToAsync(fs);

            return (stored, file.ContentType);
        }

        /* ───────────────────────── 1a) SALVAR ‒ vários arquivos ─────────────
         * Mantida apenas para código legado (PoliticasController, etc.).     */
        public async Task<string> SaveFiles(IEnumerable<IFormFile> files)
        {
            var nomes = new List<string>();

            foreach (var f in files)
            {
                var (stored, _) = await SaveFile(f);
                nomes.Add(stored);
            }
            return string.Join(';', nomes);
        }


        /* ───────────────────────── 2) EXCLUIR ─────────────────────────────── */
        public Task DeleteFile(string storedNames)
        {
            if (string.IsNullOrWhiteSpace(storedNames))
                return Task.CompletedTask;

            foreach (var name in storedNames.Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                var full = Path.Combine(_uploadPath, name.Trim());
                if (File.Exists(full)) File.Delete(full);
            }
            return Task.CompletedTask;
        }

        /* ───────────────────────── 3) EXISTS ──────────────────────────────── */
        public bool Exists(string storedName)
        {
            if (string.IsNullOrWhiteSpace(storedName)) return false;
            return File.Exists(Path.Combine(_uploadPath, storedName));
        }

        /* ───────────────────────── 4) OPEN READ ───────────────────────────── */
        public Task<Stream?> OpenReadAsync(string storedName)
        {
            var full = Path.Combine(_uploadPath, storedName ?? "");
            if (!File.Exists(full)) return Task.FromResult<Stream?>(null);

            Stream s = new FileStream(full, FileMode.Open, FileAccess.Read, FileShare.Read);
            return Task.FromResult<Stream?>(s);
        }
    }
}
