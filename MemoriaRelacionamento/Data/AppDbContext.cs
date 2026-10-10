using MemoriaRelacionamento.Models;
using Microsoft.EntityFrameworkCore;

namespace MemoriaRelacionamento.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Conversa> Conversas => Set<Conversa>();
    public DbSet<InformacaoInsight> Insights => Set<InformacaoInsight>();
    public DbSet<AcaoToDo> Acoes => Set<AcaoToDo>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Cliente>(e =>
        {
            e.ToTable("cliente");
            e.Property(c => c.CriadoEm).HasDefaultValueSql("now()");
            e.HasMany(c => c.Conversas).WithOne(c => c.Cliente).HasForeignKey(c => c.ClienteId);
        });

        b.Entity<Conversa>(e =>
        {
            e.ToTable("conversa");
            e.Property(c => c.Origem).HasConversion<string>();
            e.Property(c => c.Status).HasConversion<string>();
            e.Property(c => c.Sentimento).HasConversion<string>();
            e.Property(c => c.CriadoEm).HasDefaultValueSql("now()");
        });

        b.Entity<InformacaoInsight>(e =>
        {
            e.ToTable("informacao_insight");
            e.Property(i => i.Categoria).HasConversion<string>();
            e.Property(i => i.StatusDeRevisao).HasConversion<string>();
        });

        b.Entity<AcaoToDo>().ToTable("acao_todo");
    }
}
