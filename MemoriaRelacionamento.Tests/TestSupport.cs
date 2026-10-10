using System.Text.Json;
using MemoriaRelacionamento.Data;
using MemoriaRelacionamento.Models;
using MemoriaRelacionamento.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Logging.Abstractions;

namespace MemoriaRelacionamento.Tests;

/// <summary>AppDbContext sobre SQLite: troca arrays do Postgres por JSON e remove o default now().</summary>
public class TestDbContext : AppDbContext
{
    public TestDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);
        var conv = new ValueConverter<List<string>, string>(
            v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
            v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null)!);
        b.Entity<Conversa>().Property(c => c.Resumo).HasConversion(conv);


        var arrays = new ValueConverter<string[], string>(
            v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
            v => JsonSerializer.Deserialize<string[]>(v, (JsonSerializerOptions?)null)!);
        b.Entity<InformacaoInsight>().Property(c => c.Tags).HasConversion(arrays);
        b.Entity<InformacaoEvento>().Property(c => c.Tags).HasConversion(arrays);
        b.Entity<InformacaoFinanceira>().Property(c => c.Tags).HasConversion(arrays);
        b.Entity<AcaoToDo>().Property(c => c.Tags).HasConversion(arrays);
    }
}

public sealed class BancoDeTeste : IDisposable
{
    private readonly SqliteConnection _conn = new("DataSource=:memory:");

    public BancoDeTeste()
    {
        _conn.Open();
        using var db = Novo();
        db.Database.EnsureCreated();
    }

    public AppDbContext Novo() =>
        new TestDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_conn).Options);

    public async Task<Cliente> CriarClienteAsync(string nome = "Roberto")
    {
        await using var db = Novo();
        var c = new Cliente { Id = Guid.NewGuid(), Nome = nome, CrmId = Guid.NewGuid().ToString() };
        db.Clientes.Add(c);
        await db.SaveChangesAsync();
        return c;
    }

    public void Dispose() => _conn.Dispose();
}

public class TranscricaoStub : ITranscriptionService
{
    public int Chamadas;
    public string Texto = "texto transcrito do áudio";
    public Exception? Falha;

    public Task<string> TranscreverAsync(string caminhoAudio, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (!File.Exists(caminhoAudio)) throw new FileNotFoundException();
        Chamadas++;
        if (Falha != null) throw Falha;
        return Task.FromResult(Texto);
    }
}

public class AnaliseStub : IConversationAnalysisService
{
    public int Chamadas;
    public Exception? Falha;
    public AnaliseRequest? Ultima;
    public AnaliseConversa Resultado = new(
        new() { "um", "dois", "três" },
        Sentimento.Satisfeito, null, null,
        new() { new("Filha Marina", "a filha Marina", new() { "familia", "filha" }) },
        new(),
        new(),
        new() { new("Enviar simulação", new DateOnly(2026, 10, 20), new() { "previdencia" }) },
        new());

    public Task<AnaliseConversa> AnalisarAsync(AnaliseRequest request, CancellationToken ct)
    {
        Chamadas++;
        Ultima = request;
        if (Falha != null) throw Falha;
        return Task.FromResult(Resultado);
    }
}

public static class Fabrica
{
    public static ConversaProcessor Processor(AppDbContext db, ITranscriptionService t, IConversationAnalysisService a) =>
        new(db, t, a, NullLogger<ConversaProcessor>.Instance);

    public static async Task<Guid> ConversaAsync(BancoDeTeste banco, Guid clienteId,
        OrigemConversa origem, string? transcricao, string? audio = null)
    {
        await using var db = banco.Novo();
        var c = new Conversa
        {
            Id = Guid.NewGuid(), ClienteId = clienteId, Data = new DateOnly(2026, 10, 10),
            Origem = origem, Transcricao = transcricao, CaminhoDoAudio = audio ?? "", Status = StatusConversa.NaFila,
        };
        db.Conversas.Add(c);
        await db.SaveChangesAsync();
        return c.Id;
    }
}
