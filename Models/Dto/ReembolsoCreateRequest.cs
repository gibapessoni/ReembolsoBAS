
using System.ComponentModel.DataAnnotations;

namespace ReembolsoBAS.Models.Dto;

public class ReembolsoCreateRequest
{
    [Required] public string Matricula { get; set; } = "";
    [Required, RegularExpression(@"^\d{4}-(0[1-9]|1[0-2])$")]
    public string Periodo { get; set; } = "";   // YYYY-MM
    public List<LancamentoCreateDto> Lancamentos { get; set; } = [];
}
