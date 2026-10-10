using MemoriaRelacionamento.Models;

namespace MemoriaRelacionamento.Services.Fakes;

public class FakeConversationAnalysisService : IConversationAnalysisService
{
    public async Task<AnaliseConversa> AnalisarAsync(AnaliseRequest request, CancellationToken ct)
    {
        await Task.Delay(TimeSpan.FromSeconds(3), ct);
        return new(
            ["Resumo fictício: conversa recebida.", "A análise real ainda será integrada.", "Os dados abaixo servem apenas para testar o fluxo."],
            Sentimento.Neutro,
            [new(CategoriaInsight.Outro, "Informação fictícia para validar o perfil.", request.Transcricao[..Math.Min(120, request.Transcricao.Length)], null, false)],
            [new("Tarefa fictícia: preparar o próximo contato.", request.DataConversa.AddDays(7))], []);
    }
}
