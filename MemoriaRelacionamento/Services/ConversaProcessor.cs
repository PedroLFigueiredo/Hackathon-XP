using MemoriaRelacionamento.Data;
using MemoriaRelacionamento.Models;
using Microsoft.EntityFrameworkCore;

namespace MemoriaRelacionamento.Services;

public class ConversaProcessor(AppDbContext db, ITranscriptionService transcription, IConversationAnalysisService analysis, ILogger<ConversaProcessor> logger)
{
    public async Task ProcessarAsync(Guid id, CancellationToken ct)
    {
        var conversa = await db.Conversas.FindAsync([id], ct);
        if (conversa is null || conversa.Status == StatusConversa.Concluida) return;
        try
        {
            conversa.MensagemDeErro = null;
            var cliente = await db.Clientes.SingleAsync(x => x.Id == conversa.ClienteId, ct);
            if (string.IsNullOrWhiteSpace(conversa.Transcricao) && conversa.CaminhoDoAudio is not null)
            {
                conversa.Status = StatusConversa.Transcrevendo;
                await db.SaveChangesAsync(ct);
                conversa.Transcricao = await transcription.TranscreverAsync(conversa.CaminhoDoAudio, ct);
            }
            if (string.IsNullOrWhiteSpace(conversa.Transcricao)) throw new InvalidOperationException("Nenhuma transcrição disponível.");
            conversa.Status = StatusConversa.Analisando;
            await db.SaveChangesAsync(ct);
            var facts = await db.Insights.Where(x => x.ClienteId == cliente.Id).Select(x => x.Conteudo).ToListAsync(ct);
            var result = await analysis.AnalisarAsync(new(cliente.Nome, conversa.Data, conversa.Transcricao, facts), ct);
            // Resultado e status final são persistidos juntos, evitando saída parcial.
            conversa.Resumo = result.Resumo;
            conversa.Sentimento = result.Sentimento;
            conversa.PautasSugeridas = result.PautasSugeridas;
            foreach (var insight in result.Insights)
                db.Insights.Add(new InformacaoInsight { ClienteId = cliente.Id, ConversaId = id, Categoria = insight.Categoria, Conteudo = insight.Conteudo, TrechoDeOrigem = insight.TrechoDeOrigem, DataReferencia = insight.DataReferencia, RecorrenteAnual = insight.RecorrenteAnual });
            foreach (var action in result.Acoes)
                db.ToDos.Add(new AcaoToDo { ClienteId = cliente.Id, ConversaId = id, Descricao = action.Descricao, DataOpcional = action.Data });
            conversa.Status = StatusConversa.Concluida;
            await db.SaveChangesAsync(ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception exception)
        {
            logger.LogError(exception, "Falha ao processar conversa {ConversaId}", id);
            db.ChangeTracker.Clear();
            conversa = await db.Conversas.SingleAsync(x => x.Id == id, ct);
            conversa.Status = StatusConversa.Erro;
            conversa.MensagemDeErro = "Não foi possível processar a conversa. Verifique o arquivo e a configuração dos serviços.";
            await db.SaveChangesAsync(ct);
        }
    }
}
