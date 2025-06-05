using Microsoft.EntityFrameworkCore;
using ReembolsoBAS.Models;

namespace ReembolsoBAS.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

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
              .HasAlternateKey(e => e.Matricula); // matrícula é UNIQUE

            mb.Entity<Reembolso>()
              .HasOne(r => r.Empregado)
              .WithMany()
              .HasForeignKey(r => r.MatriculaEmpregado)
              .HasPrincipalKey(e => e.Matricula);

            /*-------------------------------------------------------------
             * 2) REEMBOLSO x LANÇAMENTO
             *------------------------------------------------------------*/
            mb.Entity<ReembolsoLancamento>()
              .HasOne(l => l.Reembolso)
              .WithMany(r => r.Lancamentos)
              .HasForeignKey(l => l.ReembolsoId);

            /*-------------------------------------------------------------
             * 3) USUARIO x EMPREGADO – vincular pela Matrícula
             *    Aqui dizemos que Usuario.Matricula é FK para Empregado.Matricula
             *------------------------------------------------------------*/
            mb.Entity<Usuario>()
              .HasOne(u => u.Empregado)
              .WithOne() // se você não tiver navegação inversa em Empregado, use sem parâmetro
              .HasForeignKey<Usuario>(u => u.Matricula)            // FK em Usuario.Matricula
              .HasPrincipalKey<Empregado>(e => e.Matricula);       // PK/AK em Empregado.Matricula
        }
    }
}
