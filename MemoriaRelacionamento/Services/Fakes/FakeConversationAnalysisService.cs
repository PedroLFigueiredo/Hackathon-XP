using MemoriaRelacionamento.Models;

namespace MemoriaRelacionamento.Services.Fakes;

public class FakeConversationAnalysisService : IConversationAnalysisService
{
    public async Task<AnaliseConversa> AnalisarAsync(AnaliseRequest request, CancellationToken ct)
    {
        await Task.Delay(TimeSpan.FromSeconds(3), ct);
        var trecho = request.Transcricao[..Math.Min(120, request.Transcricao.Length)];
        return new(
            ["Resumo fictício: conversa recebida.", "A análise real não foi usada nesta conversa.", "Os dados abaixo servem apenas para testar o fluxo."],
            Sentimento.Neutro, null, null,
            [new("Informação pessoal fictícia para validar o perfil.", trecho, ["outro"])],
            [new("Evento fictício para validar as datas.", request.DataConversa.AddDays(30), trecho, ["pontual"])],
            [new("Informação financeira fictícia para validar o perfil.", trecho, ["outro"])],
            [new("Tarefa fictícia: preparar o próximo contato.", request.DataConversa.AddDays(7), [])],
            ["Pauta fictícia: retomar o assunto da última conversa."])
        {
            InsightsAssessor = [new(TiposDeInsight.Oportunidade, "Insight fictício para validar a seção do assessor.", "Sugestão fictícia: confirmar o ponto com o cliente.", trecho)],
        };
    }
}
