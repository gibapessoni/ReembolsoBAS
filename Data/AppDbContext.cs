using Microsoft.EntityFrameworkCore;
using ReembolsoBAS.Models;

namespace ReembolsoBAS.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Empregado> Empregados { get; set; }
        public DbSet<Reembolso> Reembolsos { get; set; }
        public DbSet<PoliticaBAS> PoliticasBAS { get; set; }
        public DbSet<ReembolsoLancamento> ReembolsoLancamentos { get; set; }
        public DbSet<ReembolsoDocumento> ReembolsoDocumentos { get; set; }


        protected override void OnModelCreating(ModelBuilder mb)
        {
            // 1) Empregado ↔ Reembolso (mantém por matrícula)
            mb.Entity<Empregado>()
              .HasAlternateKey(e => e.Matricula);

            mb.Entity<Reembolso>()
              .HasOne(r => r.Empregado)
              .WithMany()
              .HasForeignKey(r => r.MatriculaEmpregado)
              .HasPrincipalKey(e => e.Matricula);

            // 2) Reembolso ↔ Lançamentos
            mb.Entity<ReembolsoLancamento>()
              .HasOne(l => l.Reembolso)
              .WithMany(r => r.Lancamentos)
              .HasForeignKey(l => l.ReembolsoId);

            // 3) Usuário ↔ Empregado pela FK numérica EmpregadoId
            mb.Entity<Usuario>()
              .HasOne(u => u.Empregado)
              .WithMany()
              .HasForeignKey(u => u.EmpregadoId)
              .OnDelete(DeleteBehavior.Cascade);

            // 4)  Criar índice em Matricula também
            mb.Entity<Usuario>()
              .HasIndex(u => u.Matricula)
              .IsUnique();

            mb.Entity<ReembolsoDocumento>()
              .HasOne(d => d.Lancamento)
              .WithMany(l => l.Documentos)
              .HasForeignKey(d => d.ReembolsoLancamentoId)
              .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
