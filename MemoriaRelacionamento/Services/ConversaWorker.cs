using MemoriaRelacionamento.Data;
using MemoriaRelacionamento.Models;
using Microsoft.EntityFrameworkCore;

namespace MemoriaRelacionamento.Services;

public class ConversaWorker(ConversaQueue queue, IServiceScopeFactory scopes, IConfiguration configuration, ILogger<ConversaWorker> logger) : BackgroundService
{
    /// <summary>
    /// O banco é compartilhado, mas o áudio fica no disco de quem o recebeu. Uma conversa que ainda depende
    /// do áudio só pode ser retomada na máquina que tem o arquivo; em outra, viraria erro de "áudio não encontrado".
    /// </summary>
    public static bool PodeRecuperar(string? caminhoAudio, string? transcricao) =>
        !string.IsNullOrWhiteSpace(transcricao) || string.IsNullOrEmpty(caminhoAudio) || File.Exists(caminhoAudio);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (configuration.GetValue("Pipeline:RecuperarNoStartup", true))
        {
            try
            {
                using var scope = scopes.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var pendentes = await db.Conversas.Where(x => x.Status == StatusConversa.NaFila || x.Status == StatusConversa.Transcrevendo || x.Status == StatusConversa.Analisando)
                    .Select(x => new { x.Id, x.CaminhoDoAudio, x.Transcricao }).ToListAsync(stoppingToken);
                var recuperaveis = pendentes.Where(x => PodeRecuperar(x.CaminhoDoAudio, x.Transcricao)).ToList();
                if (recuperaveis.Count < pendentes.Count)
                    logger.LogWarning("{Quantidade} conversa(s) pendente(s) têm o áudio em outra máquina e ficam para quem recebeu o arquivo.", pendentes.Count - recuperaveis.Count);
                foreach (var conversa in recuperaveis) queue.Enfileirar(conversa.Id);
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
