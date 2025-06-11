using System.ComponentModel.DataAnnotations;

namespace ReembolsoBAS.Models.Dto
{
    public class UploadPoliticaRequest
    {
        [Required]
        public string Codigo { get; set; } = "";

        public IFormFile Arquivo { get; set; } = null!;
    }
}
