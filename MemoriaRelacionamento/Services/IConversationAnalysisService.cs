using MemoriaRelacionamento.Models;

namespace MemoriaRelacionamento.Services;

public interface IConversationAnalysisService
{
    Task<AnaliseConversa> AnalisarAsync(AnaliseRequest request, CancellationToken ct);
}

public record AnaliseRequest(string NomeCliente, DateOnly DataConversa, string Transcricao, IReadOnlyList<string> FatosJaConhecidos);
public record AnaliseConversa(List<string> Resumo, Sentimento Sentimento, List<InsightExtraido> Insights, List<AcaoExtraida> Acoes, List<string> PautasSugeridas);
public record InsightExtraido(CategoriaInsight Categoria, string Conteudo, string TrechoDeOrigem, DateOnly? DataReferencia, bool RecorrenteAnual);
public record AcaoExtraida(string Descricao, DateOnly? Data);
