namespace MemoriaRelacionamento.Services.Fakes;

public class FakeTranscriptionService : ITranscriptionService
{
    public async Task<string> TranscreverAsync(string caminhoAudio, CancellationToken ct)
    {
        if (!File.Exists(caminhoAudio)) throw new FileNotFoundException("Áudio não encontrado.", caminhoAudio);
        await Task.Delay(TimeSpan.FromSeconds(3), ct);
        return "Transcrição fictícia de demonstração. O cliente mencionou interesse em ciclismo e pediu um retorno na próxima semana.";
    }
}
