namespace MemoriaRelacionamento.Services;

/// <summary>Recebe caminho local absoluto e retorna o texto completo em português.</summary>
public interface ITranscriptionService
{
    Task<string> TranscreverAsync(string caminhoAudio, CancellationToken ct);

    Task<string> TranscreverAsync(string caminhoAudio, Action<TranscriptionProgress> progresso, CancellationToken ct)
        => TranscreverAsync(caminhoAudio, ct);
}

public record TranscriptionProgress(string Etapa, double SegundosProcessados, double? SegundosTotais, string Transcricao)
{
    public int? Percentual => SegundosTotais is > 0
        ? (int)Math.Clamp(SegundosProcessados / SegundosTotais.Value * 100, 0, 100)
        : null;
}
