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

    public DbSet<InformacaoEvento> Eventos => Set<InformacaoEvento>();
    public DbSet<InformacaoFinanceira> Financeiro => Set<InformacaoFinanceira>();
    public DbSet<InsightAssessor> InsightsAssessor => Set<InsightAssessor>();
    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<Cliente>().ToTable("clientes");
        model.Entity<Conversa>().ToTable("conversas");
        model.Entity<InformacaoInsight>().ToTable("perfil_pessoal");
        model.Entity<AcaoToDo>().ToTable("acoes");

        model.Entity<Conversa>().Property(x => x.Status).HasConversion(
            v => v == StatusConversa.NaFila ? "aguardando" : v == StatusConversa.Transcrevendo ? "transcrevendo" : v == StatusConversa.Analisando ? "analisando" : v == StatusConversa.Concluida ? "concluído" : "erro",
 v => v == "aguardando" ? StatusConversa.NaFila : v == "transcrevendo" ? StatusConversa.Transcrevendo : v == "analisando" ? StatusConversa.Analisando : v == "concluído" || v == "concluido" ? StatusConversa.Concluida : StatusConversa.Erro);
        model.Entity<Conversa>().Property(x => x.Sentimento).HasConversion<string>();

        model.Entity<Conversa>().HasOne<Cliente>().WithMany().HasForeignKey(x => x.ClienteId).OnDelete(DeleteBehavior.Cascade);
        model.Entity<InformacaoInsight>().HasOne<Cliente>().WithMany().HasForeignKey(x => x.ClienteId).OnDelete(DeleteBehavior.Cascade);
        model.Entity<InformacaoInsight>().HasOne<Conversa>().WithMany().HasForeignKey(x => x.ConversaId).OnDelete(DeleteBehavior.Cascade);
        model.Entity<AcaoToDo>().HasOne<Cliente>().WithMany().HasForeignKey(x => x.ClienteId).OnDelete(DeleteBehavior.Cascade);
        model.Entity<AcaoToDo>().HasOne<Conversa>().WithMany().HasForeignKey(x => x.ConversaId).OnDelete(DeleteBehavior.Cascade);
        model.Entity<Conversa>().HasIndex(x => new { x.ClienteId, x.DataRegistro });
        model.Entity<InformacaoInsight>().HasIndex(x => x.ClienteId);
        model.Entity<AcaoToDo>().HasIndex(x => x.StatusConclusao);
        model.Entity<AcaoToDo>().Property(x => x.ConversaId).IsRequired();
        foreach (var entity in model.Model.GetEntityTypes())
        foreach (var property in entity.GetProperties())
            property.SetColumnName(Regex.Replace(property.Name, "([a-z0-9])([A-Z])", "$1_$2").ToLowerInvariant());
        model.Entity<InformacaoEvento>().ToTable("informacao_eventos");
        model.Entity<InformacaoFinanceira>().ToTable("informacao_financeiro");
        model.Entity<InformacaoEvento>().HasOne<Cliente>().WithMany().HasForeignKey(x => x.ClienteId).OnDelete(DeleteBehavior.Cascade);
        model.Entity<InformacaoFinanceira>().HasOne<Cliente>().WithMany().HasForeignKey(x => x.ClienteId).OnDelete(DeleteBehavior.Cascade);
        model.Entity<InsightAssessor>().ToTable("insights_assessor");
        model.Entity<InsightAssessor>().HasOne<Cliente>().WithMany().HasForeignKey(x => x.ClienteId).OnDelete(DeleteBehavior.Cascade);
        model.Entity<InsightAssessor>().HasOne<Conversa>().WithMany().HasForeignKey(x => x.ConversaId).OnDelete(DeleteBehavior.Cascade);
        model.Entity<Conversa>().Property(x => x.DataRegistro).HasColumnName("data");
        model.Entity<Conversa>().Property(x => x.CaminhoDoAudio).HasColumnName("caminho_audio").IsRequired();
        model.Entity<Conversa>().Property(x => x.Sentimento).HasColumnName("tom");
        model.Entity<Conversa>().Property(x => x.MensagemDeErro).HasColumnName("mensagem_erro");
        model.Entity<Conversa>().Property(x => x.Resumo).HasConversion(v => string.Join("\n", v), v => v == null ? new List<string>() : v.Split('\n', StringSplitOptions.RemoveEmptyEntries).ToList());
        model.Entity<InformacaoInsight>().Property(x => x.TrechoDeOrigem).HasColumnName("trecho_origem");
    }
}
