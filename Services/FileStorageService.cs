using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
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
    }
}
