namespace MemoriaRelacionamento.Services;

/// <summary>Recebe caminho local absoluto e retorna o texto completo em português.</summary>
public interface ITranscriptionService
{
    Task<string> TranscreverAsync(string caminhoAudio, CancellationToken ct);
}
