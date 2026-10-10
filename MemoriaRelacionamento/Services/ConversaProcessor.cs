using MemoriaRelacionamento.Data;
using MemoriaRelacionamento.Models;
using Microsoft.EntityFrameworkCore;

namespace MemoriaRelacionamento.Services;

public class ConversaProcessor(AppDbContext db, ITranscriptionService transcription, IConversationAnalysisService analysis, ILogger<ConversaProcessor> logger, TranscriptionProgressStore? progresso = null)
{
    public async Task ProcessarAsync(Guid id, CancellationToken ct)
    {
        var conversa = await db.Conversas.FindAsync([id], ct);
        if (conversa is null || conversa.Status == StatusConversa.Concluida) return;
        var etapa = "abrir a conversa";
        string? transcricaoCompleta = conversa.Transcricao;
        try
        {
            conversa.MensagemDeErro = null;
            var cliente = await db.Clientes.SingleAsync(x => x.Id == conversa.ClienteId, ct);
            if (string.IsNullOrWhiteSpace(conversa.Transcricao) && !string.IsNullOrEmpty(conversa.CaminhoDoAudio))
            {
                etapa = "iniciar a transcrição";
                conversa.Status = StatusConversa.Transcrevendo;
                await db.SaveChangesAsync(ct);
                progresso?.Atualizar(id, new("Preparando áudio", 0, null, ""));
                etapa = "transcrever o áudio";
                conversa.Transcricao = await transcription.TranscreverAsync(conversa.CaminhoDoAudio,
                    andamento => progresso?.Atualizar(id, andamento), ct);
                transcricaoCompleta = conversa.Transcricao;
            }
            if (string.IsNullOrWhiteSpace(conversa.Transcricao)) throw new InvalidOperationException("Nenhuma transcrição disponível.");
            etapa = "salvar a transcrição";
            conversa.Status = StatusConversa.Analisando;
            await db.SaveChangesAsync(ct);
            etapa = "analisar a conversa";
            var facts = await db.Insights.Where(x => x.ClienteId == cliente.Id).Select(x => x.Conteudo).ToListAsync(ct);
            facts.AddRange(await db.Eventos.Where(x => x.ClienteId == cliente.Id).Select(x => x.Conteudo).ToListAsync(ct));
            facts.AddRange(await db.Financeiro.Where(x => x.ClienteId == cliente.Id).Select(x => x.Conteudo).ToListAsync(ct));
            var result = await analysis.AnalisarAsync(new(cliente.Nome, conversa.Data, conversa.Transcricao, facts), ct);
            // Resultado e status final são persistidos juntos, evitando saída parcial.
            conversa.Resumo = result.Resumo;
            conversa.Sentimento = result.Sentimento;
            conversa.PautasSugeridas = result.PautasSugeridas;
            foreach (var insight in result.Insights)
            {
                if (insight.Categoria == CategoriaInsight.Evento)
                    db.Eventos.Add(new InformacaoEvento { ClienteId = cliente.Id, Conteudo = insight.Conteudo, TrechoOrigem = insight.TrechoDeOrigem, DataEvento = insight.DataReferencia });
                else if (insight.Categoria == CategoriaInsight.Objetivo)
                    db.Financeiro.Add(new InformacaoFinanceira { ClienteId = cliente.Id, Conteudo = insight.Conteudo, TrechoOrigem = insight.TrechoDeOrigem });
                else db.Insights.Add(new InformacaoInsight { ClienteId = cliente.Id, ConversaId = id, Categoria = insight.Categoria, Conteudo = insight.Conteudo, TrechoDeOrigem = insight.TrechoDeOrigem, DataReferencia = insight.DataReferencia, RecorrenteAnual = insight.RecorrenteAnual });
            }
            foreach (var action in result.Acoes)
                db.ToDos.Add(new AcaoToDo { ClienteId = cliente.Id, ConversaId = id, Descricao = action.Descricao, DataOpcional = action.Data });
            conversa.Status = StatusConversa.Concluida;
            etapa = "salvar o resultado";
            await db.SaveChangesAsync(ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception exception)
        {
            logger.LogError(exception, "Falha ao processar conversa {ConversaId} na etapa {Etapa}", id, etapa);
            db.ChangeTracker.Clear();
            conversa = await db.Conversas.SingleAsync(x => x.Id == id, ct);
            conversa.Status = StatusConversa.Erro;
            if (!string.IsNullOrWhiteSpace(transcricaoCompleta)) conversa.Transcricao = transcricaoCompleta;
            var detalhe = etapa == "transcrever o áudio" ? exception.GetBaseException().Message : exception.GetBaseException().GetType().Name;
            if (detalhe.Length > 300) detalhe = detalhe[..300];
            conversa.MensagemDeErro = $"Não foi possível {etapa}. Detalhe: {detalhe}. Tente novamente nesta conversa.";
            await db.SaveChangesAsync(ct);
        }
        finally { progresso?.Remover(id); }
    }
}
