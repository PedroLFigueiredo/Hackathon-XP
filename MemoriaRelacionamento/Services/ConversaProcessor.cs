using MemoriaRelacionamento.Data;
using MemoriaRelacionamento.Models;
using Microsoft.EntityFrameworkCore;

namespace MemoriaRelacionamento.Services;

public class ConversaProcessor
{
    private readonly AppDbContext _db;
    private readonly ITranscriptionService _transcricao;
    private readonly IConversationAnalysisService _analise;
    private readonly ILogger<ConversaProcessor> _log;

    public ConversaProcessor(
        AppDbContext db,
        ITranscriptionService transcricao,
        IConversationAnalysisService analise,
        ILogger<ConversaProcessor> log)
    {
        _db = db;
        _transcricao = transcricao;
        _analise = analise;
        _log = log;
    }

    /// <summary>Nunca lança, exceto cancelamento por shutdown (a conversa fica em status intermediário e é retomada no próximo startup).</summary>
    public async Task ProcessarAsync(Guid conversaId, CancellationToken ct)
    {
        var conversa = await _db.Conversas
            .Include(c => c.Cliente)
            .FirstOrDefaultAsync(c => c.Id == conversaId, ct);
        if (conversa is null)
        {
            _log.LogWarning("Conversa {Id} não encontrada", conversaId);
            return;
        }

        try
        {
            if (string.IsNullOrWhiteSpace(conversa.Transcricao))
            {
                if (string.IsNullOrEmpty(conversa.CaminhoDoAudio))
                    throw new InvalidOperationException("A conversa não tem transcrição nem áudio.");
                if (!File.Exists(conversa.CaminhoDoAudio))
                    throw new FileNotFoundException($"Arquivo de áudio não encontrado: {conversa.CaminhoDoAudio}");

                await DefinirStatusAsync(conversa, StatusConversa.Transcrevendo, ct);
                var texto = await _transcricao.TranscreverAsync(conversa.CaminhoDoAudio, ct);
                if (string.IsNullOrWhiteSpace(texto))
                    throw new InvalidOperationException("A transcrição voltou vazia (áudio sem fala reconhecível).");
                conversa.Transcricao = texto.Trim();
                await _db.SaveChangesAsync(ct);
            }

            await DefinirStatusAsync(conversa, StatusConversa.Analisando, ct);

            var fatos = await _db.Insights
                .Where(i => i.ClienteId == conversa.ClienteId && i.StatusDeRevisao == StatusRevisao.Aprovado)
                .Select(i => i.Conteudo)
                .ToListAsync(ct);

            var resultado = await _analise.AnalisarAsync(
                new AnaliseRequest(conversa.Cliente!.Nome, conversa.Data, conversa.Transcricao!, fatos), ct);

            await GravarResultadoAsync(conversa, resultado, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Falha ao processar a conversa {Id}", conversaId);
            _db.ChangeTracker.Clear();
            await _db.Conversas.Where(c => c.Id == conversaId).ExecuteUpdateAsync(s => s
                .SetProperty(c => c.Status, StatusConversa.Erro)
                .SetProperty(c => c.MensagemDeErro, Truncar(ex.Message)), CancellationToken.None);
        }
    }

    private async Task DefinirStatusAsync(Conversa conversa, StatusConversa status, CancellationToken ct)
    {
        conversa.Status = status;
        conversa.MensagemDeErro = null;
        await _db.SaveChangesAsync(ct);
    }

    private async Task GravarResultadoAsync(Conversa conversa, AnaliseConversa r, CancellationToken ct)
    {
        // Reprocessamento: remove o que ainda não foi revisado/concluído para não duplicar.
        await _db.Insights
            .Where(i => i.ConversaId == conversa.Id && i.StatusDeRevisao == StatusRevisao.Pendente)
            .ExecuteDeleteAsync(ct);
        await _db.Acoes
            .Where(a => a.ConversaId == conversa.Id && !a.Concluida)
            .ExecuteDeleteAsync(ct);

        conversa.Resumo = r.Resumo;
        conversa.Sentimento = r.Sentimento;
        conversa.PautasSugeridas = r.PautasSugeridas;
        conversa.Status = StatusConversa.Concluida;
        conversa.MensagemDeErro = null;

        _db.Insights.AddRange(r.Insights.Select(i => new InformacaoInsight
        {
            ClienteId = conversa.ClienteId,
            ConversaId = conversa.Id,
            Categoria = i.Categoria,
            Conteudo = i.Conteudo,
            TrechoDeOrigem = i.TrechoDeOrigem,
            DataReferencia = i.DataReferencia,
            RecorrenteAnual = i.RecorrenteAnual,
            StatusDeRevisao = StatusRevisao.Pendente,
        }));
        _db.Acoes.AddRange(r.Acoes.Select(a => new AcaoToDo
        {
            ClienteId = conversa.ClienteId,
            ConversaId = conversa.Id,
            Descricao = a.Descricao,
            DataOpcional = a.Data,
            Concluida = false,
        }));

        await _db.SaveChangesAsync(ct);
    }

    private static string Truncar(string s) => s.Length <= 500 ? s : s[..500];
}
