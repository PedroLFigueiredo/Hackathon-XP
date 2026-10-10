using MemoriaRelacionamento.Models;

namespace MemoriaRelacionamento.Services;

public class FakeTranscriptionService : ITranscriptionService
{
    public async Task<string> TranscreverAsync(string caminhoAudio, CancellationToken ct)
    {
        await Task.Delay(TimeSpan.FromSeconds(3), ct);
        return "Conversei com o cliente sobre a viagem de fim de ano com a família. " +
               "A filha Marina faz aniversário no dia 12 de março e ele quer planejar a festa. " +
               "Ele pediu para enviar uma simulação de aporte mensal até a próxima semana.";
    }
}

public class FakeConversationAnalysisService : IConversationAnalysisService
{
    public async Task<AnaliseConversa> AnalisarAsync(AnaliseRequest request, CancellationToken ct)
    {
        await Task.Delay(TimeSpan.FromSeconds(3), ct);
        return new AnaliseConversa(
            Resumo: new() { "Planejamento da viagem de fim de ano", "Aniversário da filha Marina", "Simulação de aporte mensal" },
            Sentimento: Sentimento.Satisfeito,
            Insights: new()
            {
                new(CategoriaInsight.Familia, "Filha Marina faz aniversário em 12 de março", "A filha Marina faz aniversário no dia 12 de março", new DateOnly(request.DataConversa.Year + 1, 3, 12), true),
                new(CategoriaInsight.Evento, "Viagem em família no fim de ano", "viagem de fim de ano com a família", null, false),
            },
            Acoes: new() { new("Enviar simulação de aporte mensal", request.DataConversa.AddDays(7)) },
            PautasSugeridas: new() { "Perguntar como foi a viagem de fim de ano", "Parabenizar pelo aniversário da Marina" });
    }
}

/// <summary>Placeholder até o plano 03: a conversa vai para Erro com uma mensagem clara.</summary>
public class AnaliseNaoConfiguradaService : IConversationAnalysisService
{
    public Task<AnaliseConversa> AnalisarAsync(AnaliseRequest request, CancellationToken ct) =>
        throw new NotSupportedException(
            "Análise por IA não configurada (plano 03). Ligue Fakes:Analise=true ou implemente o serviço da OpenAI.");
}
