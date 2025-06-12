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

        // ─────────────────────────────────────────────────────────────────────────────
        // 1) SALVAR
        // Retorna uma string “nome1;nome2;nome3”
        // ─────────────────────────────────────────────────────────────────────────────
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

            return string.Join(";", fileNames);
        }

        // ─────────────────────────────────────────────────────────────────────────────
        // 2) EXCLUIR
        // Recebe a mesma string retornada acima e apaga todos os arquivos listados.
        // ─────────────────────────────────────────────────────────────────────────────
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
                {
                    File.Delete(fullPath);
                }
            }

            return Task.CompletedTask;  
        }
    }
}
