using MemoriaRelacionamento.Services;
using NAudio.Wave;

namespace MemoriaRelacionamento.Tests;

public class AudioNormalizerTests
{
    private static string GerarWav(int sampleRate, int canais, double segundos)
    {
        var caminho = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".wav");
        using var w = new WaveFileWriter(caminho, new WaveFormat(sampleRate, 16, canais));
        var quadros = (int)(sampleRate * segundos);
        for (var i = 0; i < quadros; i++)
        {
            var amostra = (float)(Math.Sin(2 * Math.PI * 440 * i / sampleRate) * 0.25);
            for (var c = 0; c < canais; c++) w.WriteSample(amostra);
        }
        return caminho;
    }

    [Theory]
    [InlineData(44100, 2)]
    [InlineData(48000, 1)]
    [InlineData(16000, 1)]
    public void Converte_para_wav_16k_mono_16bits_preservando_a_duracao(int sampleRate, int canais)
    {
        var origem = GerarWav(sampleRate, canais, 2.0);
        var destino = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".wav");
        try
        {
            AudioNormalizer.ParaWav16kMono(origem, destino);

            using var r = new WaveFileReader(destino);
            Assert.Equal(16000, r.WaveFormat.SampleRate);
            Assert.Equal(1, r.WaveFormat.Channels);
            Assert.Equal(16, r.WaveFormat.BitsPerSample);
            Assert.InRange(r.TotalTime.TotalSeconds, 1.9, 2.1);
        }
        finally
        {
            File.Delete(origem);
            File.Delete(destino);
        }
    }

    [Fact]
    public void Arquivo_corrompido_lanca()
    {
        var origem = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".mp3");
        File.WriteAllText(origem, "isto nao e um mp3");
        try
        {
            Assert.ThrowsAny<Exception>(() =>
                AudioNormalizer.ParaWav16kMono(origem, Path.Combine(Path.GetTempPath(), "x.wav")));
        }
        finally { File.Delete(origem); }
    }
}
