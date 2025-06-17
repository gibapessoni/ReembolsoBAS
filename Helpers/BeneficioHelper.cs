namespace ReembolsoBAS.Helpers;

public static class BeneficioHelper
{
    public static decimal CalcularLimite(string cargo, IConfiguration cfg)
    {
        var sec = cfg.GetSection("Beneficio");
        return cargo switch
        {
            "Diretor-Presidente" => sec.GetValue<decimal>("LimiteDiretorPresidente"),
            "Diretor" => sec.GetValue<decimal>("LimiteDiretor"),
            "Colaborador" or "Cedido" or _ => sec.GetValue<decimal>("LimiteEmpregado")
        };
    }
}