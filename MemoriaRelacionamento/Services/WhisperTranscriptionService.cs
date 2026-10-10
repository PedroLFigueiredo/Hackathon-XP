using System.Text;
using NAudio.Wave;
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

    public Task<string> TranscreverAsync(string caminhoAudio, CancellationToken ct)
        => TranscreverAsync(caminhoAudio, _ => { }, ct);

    public async Task<string> TranscreverAsync(string caminhoAudio, Action<TranscriptionProgress> progresso, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (!File.Exists(caminhoAudio))
            throw new FileNotFoundException("Áudio não encontrado.", caminhoAudio);
        var cache = LocalTranscriptCache.Ler(caminhoAudio);
        if (cache is not null)
        {
            _log.LogInformation("Reutilizando transcrição completa de {Arquivo}", Path.GetFileName(caminhoAudio));
            return cache;
        }
        var temporario = Path.Combine(Path.GetTempPath(), $"whisper-{Guid.NewGuid():N}.wav");
        var texto = new StringBuilder();
        var concluida = false;
        try
        {
            var inicio = DateTime.UtcNow;
            progresso(new("Preparando áudio", 0, null, ""));
            _log.LogInformation("Preparando áudio {Arquivo}", Path.GetFileName(caminhoAudio));
            // Decodificação síncrona e pesada: sai da thread do worker.
            await Task.Run(() => AudioNormalizer.ParaWav16kMono(caminhoAudio, temporario), ct);
            ct.ThrowIfCancellationRequested();

            double duracao;
            using (var reader = new WaveFileReader(temporario)) duracao = reader.TotalTime.TotalSeconds;
            progresso(new("Transcrevendo áudio", 0, duracao, ""));
            _log.LogInformation("Iniciando transcrição de {Arquivo}: {Duracao:F0} segundos de áudio",
                Path.GetFileName(caminhoAudio), duracao);

            // O processor é criado por transcrição; a factory (modelo carregado) é compartilhada.
            await using var processor = _factory.CreateBuilder().WithLanguage("pt").Build();
            await using var wav = File.OpenRead(temporario);

            var ultimoLog = DateTime.UtcNow;
            await foreach (var segmento in processor.ProcessAsync(wav, ct))
            {
                texto.Append(segmento.Text).Append(' ');
                var andamento = new TranscriptionProgress("Transcrevendo áudio", segmento.End.TotalSeconds, duracao, texto.ToString().Trim());
                progresso(andamento);
                if ((DateTime.UtcNow - ultimoLog).TotalSeconds >= 10)
                {
                    SalvarBackup(caminhoAudio, andamento.Transcricao, parcial: true);
                    _log.LogInformation("Transcrição de {Arquivo}: {Percentual}% ({Processados:F0}/{Total:F0}s)",
                        Path.GetFileName(caminhoAudio), andamento.Percentual, andamento.SegundosProcessados, duracao);
                    ultimoLog = DateTime.UtcNow;
                }
            }

            _log.LogInformation("Transcrição de {Arquivo} levou {Seg:F1}s", Path.GetFileName(caminhoAudio),
                (DateTime.UtcNow - inicio).TotalSeconds);
            SalvarBackup(caminhoAudio, texto.ToString().Trim(), parcial: false);
            concluida = true;
            return texto.ToString().Trim();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new InvalidOperationException(
                $"Não foi possível transcrever o áudio: {ex.Message}", ex);
        }
        finally
        {
            if (!concluida && texto.Length > 0) SalvarBackup(caminhoAudio, texto.ToString().Trim(), parcial: true);
            try { if (File.Exists(temporario)) File.Delete(temporario); }
            catch (IOException ex) { _log.LogWarning(ex, "Não foi possível remover o WAV temporário"); }
        }
    }

    private void SalvarBackup(string audio, string texto, bool parcial)
    {
        try { LocalTranscriptCache.Salvar(audio, texto, parcial); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _log.LogWarning(ex, "Não foi possível preservar a transcrição de {Arquivo}", Path.GetFileName(audio));
        }
    }
}
