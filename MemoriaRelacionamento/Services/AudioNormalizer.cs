using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace MemoriaRelacionamento.Services;

public static class AudioNormalizer
{
    /// <summary>Converte WAV/MP3/M4A para WAV 16 kHz, mono, 16 bits (formato exigido pelo Whisper).</summary>
    public static void ParaWav16kMono(string origem, string destino)
    {
        using var leitor = new MediaFoundationReader(origem);
        ISampleProvider amostras = leitor.ToSampleProvider();

        if (amostras.WaveFormat.Channels > 1)
        {
            // Média dos canais; cobre estéreo e layouts com mais canais.
            amostras = new MultiChannelToMono(amostras);
        }

        var reamostrado = new WdlResamplingSampleProvider(amostras, 16000);
        WaveFileWriter.CreateWaveFile16(destino, reamostrado);
    }

    private sealed class MultiChannelToMono : ISampleProvider
    {
        private readonly ISampleProvider _fonte;
        private float[] _buffer = Array.Empty<float>();

        public MultiChannelToMono(ISampleProvider fonte)
        {
            _fonte = fonte;
            WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(fonte.WaveFormat.SampleRate, 1);
        }

        public WaveFormat WaveFormat { get; }

        public int Read(float[] buffer, int offset, int count)
        {
            var canais = _fonte.WaveFormat.Channels;
            var necessario = count * canais;
            if (_buffer.Length < necessario) _buffer = new float[necessario];

            var lidos = _fonte.Read(_buffer, 0, necessario);
            var quadros = lidos / canais;
            for (var i = 0; i < quadros; i++)
            {
                float soma = 0;
                for (var c = 0; c < canais; c++) soma += _buffer[i * canais + c];
                buffer[offset + i] = soma / canais;
            }
            return quadros;
        }
    }
}
