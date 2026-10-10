using MemoriaRelacionamento.Data;
using MemoriaRelacionamento.Models;
using Microsoft.EntityFrameworkCore;

namespace MemoriaRelacionamento.Services;

public class ConversaWorker(ConversaQueue queue, IServiceScopeFactory scopes, IConfiguration configuration, ILogger<ConversaWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (configuration.GetValue("Pipeline:RecuperarNoStartup", true))
        {
            try
            {
                using var scope = scopes.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var ids = await db.Conversas.Where(x => x.Status == StatusConversa.NaFila || x.Status == StatusConversa.Transcrevendo || x.Status == StatusConversa.Analisando).Select(x => x.Id).ToListAsync(stoppingToken);
                foreach (var id in ids) queue.Enfileirar(id);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
            catch (Exception exception) { logger.LogError(exception, "Não foi possível recuperar a fila. Verifique o Supabase e o schema."); }
        }
        try
        {
            await foreach (var id in queue.LerAsync(stoppingToken))
            {
                try
                {
                    using var scope = scopes.CreateScope();
                    await scope.ServiceProvider.GetRequiredService<ConversaProcessor>().ProcessarAsync(id, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
                catch (Exception exception) { logger.LogError(exception, "Falha no job {ConversaId}", id); }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
    }
}
