using MemoriaRelacionamento.Models;

namespace MemoriaRelacionamento.Services;

public interface IConversationAnalysisService
{
    Task<AnaliseConversa> AnalisarAsync(AnaliseRequest request, CancellationToken ct);
}

public record AnaliseRequest(string NomeCliente, DateOnly DataConversa, string Transcricao, IReadOnlyList<string> FatosJaConhecidos,
    string? PerfilFinanceiroCadastrado = null);

/// <summary>Resultado da análise, no formato das tabelas do schema. A primeira tag de cada item é a categoria.</summary>
public record AnaliseConversa(
    List<string> Resumo,
    Sentimento? Sentimento,
    string? Personalidade,
    string? PerfilFinanceiro,
    List<InformacaoExtraida> PerfilPessoal,
    List<EventoExtraido> Eventos,
    List<InformacaoExtraida> Financeiro,
    List<AcaoExtraida> Acoes,
    List<string> PautasSugeridas)
{
    /// <summary>Sinais para o assessor avaliar. Não são fatos do cliente nem recomendação de investimento.</summary>
    public List<InsightParaAssessor> InsightsAssessor { get; init; } = [];

    public static AnaliseConversa Vazia => new([], null, null, null, [], [], [], [], []);
}

public record InsightParaAssessor(string Tipo, string Conteudo, string Sugestao, string TrechoOrigem);
public record InformacaoExtraida(string Conteudo, string TrechoOrigem, List<string> Tags);
public record EventoExtraido(string Conteudo, DateOnly? DataEvento, string TrechoOrigem, List<string> Tags);
public record AcaoExtraida(string Descricao, DateOnly? Data, List<string> Tags);

public static class TagsReservadas
{
    public const string Pauta = "pauta";
    public const string Tarefa = "tarefa";
    public const string Anual = "anual";
}

/// <summary>Falha da análise com mensagem que pode ser mostrada ao assessor.</summary>
public class AnaliseException(string message, Exception? inner = null) : Exception(message, inner);
