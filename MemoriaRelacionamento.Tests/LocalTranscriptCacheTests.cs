using MemoriaRelacionamento.Services;

namespace MemoriaRelacionamento.Tests;

public class LocalTranscriptCacheTests
{
    [Fact]
    public void Transcricao_parcial_nao_e_reutilizada_como_completa()
    {
        var audio = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".m4a");
        try
        {
            LocalTranscriptCache.Salvar(audio, "Primeiro trecho", parcial: true);
            Assert.Null(LocalTranscriptCache.Ler(audio));
            Assert.Equal("Primeiro trecho", LocalTranscriptCache.Ler(audio, parcial: true));
            LocalTranscriptCache.Salvar(audio, "Primeiro trecho. Segundo trecho.");
            Assert.Equal("Primeiro trecho. Segundo trecho.", LocalTranscriptCache.Ler(audio));
        }
        finally
        {
            File.Delete(audio + ".parcial.txt");
            File.Delete(audio + ".transcricao.txt");
        }
    }
}
