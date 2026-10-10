using MemoriaRelacionamento.Services;

namespace MemoriaRelacionamento.Tests;

public class TranscriptOnlyAnalysisTests
{
    [Fact]
    public async Task Nao_inventa_fatos_tarefas_resumo_ou_sentimento()
    {
        var result = await new TranscriptOnlyAnalysisService().AnalisarAsync(
            new("Ana", new DateOnly(2026, 10, 10), "Eu gostaria de falar sobre minha conta.", []), default);

        Assert.Empty(result.Resumo);
        Assert.Empty(result.PerfilPessoal);
        Assert.Empty(result.Eventos);
        Assert.Empty(result.Financeiro);
        Assert.Null(result.Personalidade);
        Assert.Null(result.PerfilFinanceiro);
        Assert.Empty(result.Acoes);
        Assert.Empty(result.PautasSugeridas);
        Assert.Null(result.Sentimento);
    }
}
