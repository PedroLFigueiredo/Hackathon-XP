using MemoriaRelacionamento.Data;
using MemoriaRelacionamento.Models;
using MemoriaRelacionamento.Services;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Whisper.net;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Supabase")
    ?? throw new InvalidOperationException(
        "Connection string 'ConnectionStrings:Supabase' não configurada. Use: dotnet user-secrets set \"ConnectionStrings:Supabase\" \"Host=...\"");

// --init-db: aplica db/schema.sql (e db/seed.sql, se existir) e encerra.
if (args.Contains("--init-db"))
{
    await using var conn = new NpgsqlConnection(connectionString);
    await conn.OpenAsync();
    foreach (var arquivo in new[] { "schema.sql", "seed.sql" })
    {
        var caminho = Path.Combine(builder.Environment.ContentRootPath, "db", arquivo);
        if (!File.Exists(caminho)) continue;
        await using var cmd = new NpgsqlCommand(await File.ReadAllTextAsync(caminho), conn);
        await cmd.ExecuteNonQueryAsync();
        Console.WriteLine($"Aplicado {arquivo}");
    }
    return;
}

builder.Services.AddRazorPages();
builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseNpgsql(connectionString).UseSnakeCaseNamingConvention());

builder.Services.AddSingleton<ConversaQueue>();
builder.Services.AddScoped<ConversaProcessor>();
builder.Services.AddScoped<ConversaService>();
builder.Services.AddHostedService<ConversaWorker>();

// Transcrição: fake ou Whisper offline.
if (builder.Configuration.GetValue("Fakes:Transcricao", false))
{
    builder.Services.AddSingleton<ITranscriptionService, FakeTranscriptionService>();
}
else
{
    var modelo = builder.Configuration["Whisper:ModelPath"] ?? "whisper-models/ggml-small.bin";
    if (!Path.IsPathRooted(modelo)) modelo = Path.Combine(builder.Environment.ContentRootPath, modelo);
    if (!File.Exists(modelo))
        throw new FileNotFoundException(
            $"Modelo Whisper não encontrado em {modelo}. Baixe com: " +
            "curl -L -o whisper-models/ggml-small.bin https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-small.bin");

    // Carregar o modelo é a parte cara: uma factory para a aplicação inteira.
    builder.Services.AddSingleton(_ => WhisperFactory.FromPath(modelo));
    builder.Services.AddSingleton<ITranscriptionService, WhisperTranscriptionService>();
}

// Análise: fake ou (plano 03, ainda não implementado) serviço real.
if (builder.Configuration.GetValue("Fakes:Analise", false))
    builder.Services.AddSingleton<IConversationAnalysisService, FakeConversationAnalysisService>();
else
    builder.Services.AddSingleton<IConversationAnalysisService, AnaliseNaoConfiguradaService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapGet("/api/conversas/{id:guid}/status", async (Guid id, AppDbContext db, CancellationToken ct) =>
{
    var c = await db.Conversas.AsNoTracking()
        .Where(x => x.Id == id)
        .Select(x => new { status = x.Status.ToString(), mensagemDeErro = x.MensagemDeErro })
        .FirstOrDefaultAsync(ct);
    return c is null ? Results.NotFound() : Results.Ok(c);
});

app.MapRazorPages();

using (var escopo = app.Services.CreateScope())
{
    var db = escopo.ServiceProvider.GetRequiredService<AppDbContext>();
    var n = await db.Clientes.CountAsync();
    app.Logger.LogInformation("Banco OK: {N} clientes", n);
}

app.Run();
