using System.Text;
using Whisper.net;

namespace MemoriaRelacionamento.Services;

public class WhisperTranscriptionService : ITranscriptionService
{
    private readonly WhisperFactory _factory;
    private readonly ILogger<WhisperTranscriptionService> _log;

    public WhisperTranscriptionService(WhisperFactory factory, ILogger<WhisperTranscriptionService> log)
    {
        _factory = factory;
        _log = log;
    }

    public async Task<string> TranscreverAsync(string caminhoAudio, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (!File.Exists(caminhoAudio))
            throw new FileNotFoundException("Áudio não encontrado.", caminhoAudio);
        var temporario = Path.Combine(Path.GetTempPath(), $"whisper-{Guid.NewGuid():N}.wav");
        try
        {
            var inicio = DateTime.UtcNow;
            // Decodificação síncrona e pesada: sai da thread do worker.
            await Task.Run(() => AudioNormalizer.ParaWav16kMono(caminhoAudio, temporario), ct);
            ct.ThrowIfCancellationRequested();

            // O processor é criado por transcrição; a factory (modelo carregado) é compartilhada.
            await using var processor = _factory.CreateBuilder().WithLanguage("pt").Build();
            await using var wav = File.OpenRead(temporario);

            var texto = new StringBuilder();
            await foreach (var segmento in processor.ProcessAsync(wav, ct))
                texto.Append(segmento.Text).Append(' ');

            _log.LogInformation("Transcrição de {Arquivo} levou {Seg:F1}s", Path.GetFileName(caminhoAudio),
                (DateTime.UtcNow - inicio).TotalSeconds);
            return texto.ToString().Trim();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new InvalidOperationException(
                $"Não foi possível transcrever o áudio (arquivo corrompido ou formato inválido?): {ex.Message}", ex);
        }
        finally
        {
            if (File.Exists(temporario)) File.Delete(temporario);
        }
    }
}
