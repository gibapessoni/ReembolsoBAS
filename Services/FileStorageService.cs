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

        /* ───────────────────────────── 1) SALVAR ───────────────────────────── */
        // Retorna uma string “nome1;nome2;nome3”
        public async Task<string> SaveFiles(IFormFileCollection files)
        {
            var fileNames = new List<string>();

            if (!Directory.Exists(_uploadPath))
                Directory.CreateDirectory(_uploadPath);

            foreach (var file in files)
            {
                var fileName = $"{Guid.NewGuid()}_{file.FileName}";
                var filePath = Path.Combine(_uploadPath, fileName);

                await using var stream = new FileStream(filePath, FileMode.Create);
                await file.CopyToAsync(stream);

                fileNames.Add(fileName);
            }

            return string.Join(';', fileNames);
        }

        /* ───────────────────────────── 2) EXCLUIR ──────────────────────────── */
        public Task DeleteFile(string storedNames)
        {
            if (string.IsNullOrWhiteSpace(storedNames))
                return Task.CompletedTask;

            var names = storedNames
                        .Split(';', StringSplitOptions.RemoveEmptyEntries)
                        .Select(n => n.Trim());

            foreach (var name in names)
            {
                var fullPath = Path.Combine(_uploadPath, name);
                if (File.Exists(fullPath))
                    File.Delete(fullPath);
            }

            return Task.CompletedTask;
        }

        /* ───────────────────────────── 3) EXISTS ───────────────────────────── */
        public bool Exists(string storedName)
        {
            if (string.IsNullOrWhiteSpace(storedName)) return false;
            var full = Path.Combine(_uploadPath, storedName);
            return File.Exists(full);
        }

        /* ───────────────────────────── 4) OPEN READ ────────────────────────── */
        public Task<Stream?> OpenReadAsync(string storedName)
        {
            if (string.IsNullOrWhiteSpace(storedName))
                return Task.FromResult<Stream?>(null);

            var full = Path.Combine(_uploadPath, storedName);

            if (!File.Exists(full))
                return Task.FromResult<Stream?>(null);

            // Abrir em modo somente-leitura compartilhado
            Stream stream = new FileStream(full, FileMode.Open, FileAccess.Read, FileShare.Read);
            return Task.FromResult<Stream?>(stream);
        }
    }
}
