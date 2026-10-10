using System.Text.RegularExpressions;
using MemoriaRelacionamento.Models;
using Microsoft.EntityFrameworkCore;

namespace MemoriaRelacionamento.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Conversa> Conversas => Set<Conversa>();
    public DbSet<InformacaoInsight> Insights => Set<InformacaoInsight>();
    public DbSet<AcaoToDo> ToDos => Set<AcaoToDo>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<Cliente>().ToTable("cliente").HasIndex(x => x.CrmId).IsUnique();
        model.Entity<Conversa>().ToTable("conversa");
        model.Entity<InformacaoInsight>().ToTable("informacao_insight");
        model.Entity<AcaoToDo>().ToTable("acao_todo");
        model.Entity<Conversa>().Property(x => x.Origem).HasConversion<string>();
        model.Entity<Conversa>().Property(x => x.Status).HasConversion<string>();
        model.Entity<Conversa>().Property(x => x.Sentimento).HasConversion<string>();
        model.Entity<InformacaoInsight>().Property(x => x.Categoria).HasConversion<string>();
        model.Entity<Conversa>().HasOne<Cliente>().WithMany().HasForeignKey(x => x.ClienteId).OnDelete(DeleteBehavior.Cascade);
        model.Entity<InformacaoInsight>().HasOne<Cliente>().WithMany().HasForeignKey(x => x.ClienteId).OnDelete(DeleteBehavior.Cascade);
        model.Entity<InformacaoInsight>().HasOne<Conversa>().WithMany().HasForeignKey(x => x.ConversaId).OnDelete(DeleteBehavior.Cascade);
        model.Entity<AcaoToDo>().HasOne<Cliente>().WithMany().HasForeignKey(x => x.ClienteId).OnDelete(DeleteBehavior.Cascade);
        model.Entity<AcaoToDo>().HasOne<Conversa>().WithMany().HasForeignKey(x => x.ConversaId).OnDelete(DeleteBehavior.Cascade);
        model.Entity<Conversa>().HasIndex(x => new { x.ClienteId, x.Data });
        model.Entity<InformacaoInsight>().HasIndex(x => x.ClienteId);
        model.Entity<AcaoToDo>().HasIndex(x => x.Concluida);
        foreach (var entity in model.Model.GetEntityTypes())
        foreach (var property in entity.GetProperties())
            property.SetColumnName(Regex.Replace(property.Name, "([a-z0-9])([A-Z])", "$1_$2").ToLowerInvariant());
    }
}
