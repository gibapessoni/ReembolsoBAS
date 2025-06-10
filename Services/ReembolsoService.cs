using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ReembolsoBAS.Data;
using ReembolsoBAS.Models;
using System;
using System.Threading.Tasks;

namespace ReembolsoBAS.Services
{
    public class ReembolsoService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _cfg;

        public ReembolsoService(AppDbContext context, IConfiguration cfg)
        {
            _context = context;
            _cfg = cfg;
        }
        public async Task ValidarReembolso(int reembolsoId)
        {
            var r = await _context.Reembolsos
                                  .Include(x => x.Empregado)
                                  .FirstOrDefaultAsync(x => x.Id == reembolsoId);
            if (r == null)
                throw new Exception("Reembolso não encontrado");

            // obtém o usuário para ler o perfil
            var u = await _context.Usuarios
                                  .FirstOrDefaultAsync(x => x.Matricula == r.MatriculaEmpregado);
            if (u == null)
                throw new Exception("Usuário não encontrado");

            // le valores do appsettings
            var limPresidente = _cfg.GetValue<decimal>("Beneficio:LimiteDiretorPresidente");
            var limDiretor = _cfg.GetValue<decimal>("Beneficio:LimiteDiretor");
            var limEmpregado = _cfg.GetValue<decimal>("Beneficio:LimiteEmpregado");

            decimal limite;
            switch (u.Perfil.ToLowerInvariant())
            {
                case "diretor-presidente":
                    limite = limPresidente;
                    break;
                case "diretor":
                    limite = limDiretor;
                    break;
                default:
                    var metade = r.Empregado.ValorMaximoMensal * 0.5m;
                    limite = Math.Min(metade, limEmpregado);
                    break;
            }

            if (r.ValorSolicitado > limite)
                throw new Exception(
                  $"Valor solicitado ({r.ValorSolicitado:C}) excede o limite de {limite:C} para perfil '{u.Perfil}'.");

            r.Status = StatusReembolso.ValidadoRH;
            await _context.SaveChangesAsync();
        }
        public async Task AprovarReembolso(int reembolsoId)
        {
            var reembolso = await _context.Reembolsos.FindAsync(reembolsoId);
            if (reembolso == null) throw new Exception("Reembolso não encontrado");

            reembolso.Status = StatusReembolso.Aprovado;
            reembolso.ValorReembolsado = reembolso.ValorSolicitado; 
            await _context.SaveChangesAsync();
        }

        public async Task ReprovarReembolso(int reembolsoId, string motivo)
        {
            var reembolso = await _context.Reembolsos.FindAsync(reembolsoId);
            if (reembolso == null) throw new Exception("Reembolso não encontrado");

            reembolso.Status = StatusReembolso.Reprovado;
            reembolso.MotivoReprovacao = motivo;
            await _context.SaveChangesAsync();
        }

        public async Task DevolverParaCorrecao(int reembolsoId, string motivo)
        {
            var reembolso = await _context.Reembolsos.FindAsync(reembolsoId);
            if (reembolso == null) throw new Exception("Reembolso não encontrado");

            reembolso.Status = StatusReembolso.DevolvidoRH;
            reembolso.MotivoReprovacao = motivo;
            await _context.SaveChangesAsync();
        }
    }
}
