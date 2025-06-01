using Microsoft.EntityFrameworkCore;
using ReembolsoBAS.Models;

namespace ReembolsoBAS.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Empregado> Empregados { get; set; }
        public DbSet<Reembolso> Reembolsos { get; set; }
        public DbSet<PoliticaBAS> PoliticasBAS { get; set; }

        public DbSet<ReembolsoLancamento> ReembolsoLancamentos { get; set; }

        protected override void OnModelCreating(ModelBuilder mb)
        {
            /*-------------------------------------------------------------
             * 1) EMPREGADO x REEMBOLSO – chave pela 'Matricula'
             *------------------------------------------------------------*/
            mb.Entity<Empregado>()
              .HasAlternateKey(e => e.Matricula);      // matrícula passa a ser chave alterna (UNIQUE)

            mb.Entity<Reembolso>()
              .HasOne(r => r.Empregado)
              .WithMany()                              // ou .WithMany(e => e.Reembolsos) se quiser navegação inversa
              .HasForeignKey(r => r.MatriculaEmpregado)
              .HasPrincipalKey(e => e.Matricula);      // *** ponto crítico ***

            /*-------------------------------------------------------------
             * 2) REEMBOLSO x LANÇAMENTO – já existia
             *------------------------------------------------------------*/
            mb.Entity<ReembolsoLancamento>()
              .HasOne(l => l.Reembolso)
              .WithMany(r => r.Lancamentos)
              .HasForeignKey(l => l.ReembolsoId);
        }



    }

}