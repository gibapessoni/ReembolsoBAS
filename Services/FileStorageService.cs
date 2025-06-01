using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace ReembolsoBAS.Services
{
    public class FileStorageService
    {
        private readonly string _uploadPath = "Uploads";

        public async Task<string> SaveFiles(IFormFileCollection files)
        {
            var fileNames = new List<string>();
            foreach (var file in files)
            {
                var fileName = $"{Guid.NewGuid()}_{file.FileName}";
                var filePath = Path.Combine(_uploadPath, fileName);

                Directory.CreateDirectory(_uploadPath);
                await using var stream = new FileStream(filePath, FileMode.Create);
                await file.CopyToAsync(stream);
                fileNames.Add(fileName);
            }
            return string.Join(";", fileNames);
        }
    }
}