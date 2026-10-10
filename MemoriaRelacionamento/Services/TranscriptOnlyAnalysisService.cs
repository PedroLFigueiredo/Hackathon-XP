namespace MemoriaRelacionamento.Services;

/// <summary>Preserva a transcrição sem gerar fatos enquanto a análise real não estiver integrada.</summary>
public sealed class TranscriptOnlyAnalysisService : IConversationAnalysisService
{
    public Task<AnaliseConversa> AnalisarAsync(AnaliseRequest request, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(AnaliseConversa.Vazia);
    }
}
