using MemoriaRelacionamento.Data;
using MemoriaRelacionamento.Models;
using Microsoft.EntityFrameworkCore;

namespace MemoriaRelacionamento.Services;

public class ConversaWorker : BackgroundService
{
    private readonly ConversaQueue _fila;
    private readonly IServiceScopeFactory _scopes;
    private readonly IConfiguration _config;
    private readonly ILogger<ConversaWorker> _log;

    public ConversaWorker(
        ConversaQueue fila,
        IServiceScopeFactory scopes,
        IConfiguration config,
        ILogger<ConversaWorker> log)
    {
        _fila = fila;
        _scopes = scopes;
        _config = config;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        if (_config.GetValue("Pipeline:RecuperarNoStartup", true))
            await RecuperarPendentesAsync(ct);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                var id = await _fila.LerAsync(ct);
                using var escopo = _scopes.CreateScope();
                var processor = escopo.ServiceProvider.GetRequiredService<ConversaProcessor>();
                await processor.ProcessarAsync(id, ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Erro inesperado no worker de conversas");
            }
        }
    }

    private async Task RecuperarPendentesAsync(CancellationToken ct)
    {
        try
        {
            using var escopo = _scopes.CreateScope();
            var db = escopo.ServiceProvider.GetRequiredService<AppDbContext>();
            var ids = await db.Conversas
                .Where(c => c.Status == StatusConversa.NaFila
                         || c.Status == StatusConversa.Transcrevendo
                         || c.Status == StatusConversa.Analisando)
                .OrderBy(c => c.CriadoEm)
                .Select(c => c.Id)
                .ToListAsync(ct);
            foreach (var id in ids) _fila.Enfileirar(id);
            if (ids.Count > 0) _log.LogInformation("Recuperadas {N} conversas pendentes", ids.Count);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _log.LogError(ex, "Falha ao recuperar conversas pendentes no startup");
        }
    }
}
