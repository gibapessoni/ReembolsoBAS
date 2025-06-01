using Microsoft.EntityFrameworkCore;
using ReembolsoBAS.Data;
using ReembolsoBAS.Models;

namespace ReembolsoBAS.Services
{
    public class ReembolsoService
    {
        private readonly AppDbContext _context;

        public ReembolsoService(AppDbContext context)
        {
            _context = context;
        }
        public async Task ValidarReembolso(int reembolsoId)
        {
            var reembolso = await _context.Reembolsos
                .Include(r => r.Empregado) 
                .FirstOrDefaultAsync(r => r.Id == reembolsoId);

            if (reembolso == null)
                throw new Exception("Reembolso não encontrado");

            if (reembolso.ValorSolicitado > reembolso.Empregado.ValorMaximoMensal)
            {
                throw new Exception("Valor solicitado excede o limite mensal");
            }

            reembolso.Status = StatusReembolso.ValidadoRH;
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
