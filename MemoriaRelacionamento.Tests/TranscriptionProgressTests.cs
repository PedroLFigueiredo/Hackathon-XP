using MemoriaRelacionamento.Models;
using MemoriaRelacionamento.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace MemoriaRelacionamento.Tests;

public class TranscriptionProgressTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Mostra_texto_parcial_sem_persistir_como_transcricao_completa(bool cancelar)
    {
        using var banco = new BancoDeTeste();
        var cliente = await banco.CriarClienteAsync();
        var id = await Fabrica.ConversaAsync(banco, cliente.Id, OrigemConversa.Audio, null, "audio.m4a");
        var store = new TranscriptionProgressStore();
        var transcription = new TranscricaoEmAndamento();
        await using var db = banco.Novo();
        using var cts = new CancellationTokenSource();
        var processor = new ConversaProcessor(db, transcription, new TranscriptOnlyAnalysisService(),
            NullLogger<ConversaProcessor>.Instance, store);

        var processamento = processor.ProcessarAsync(id, cts.Token);
        await transcription.Iniciou.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var progresso = Assert.IsType<TranscriptionProgress>(store.Obter(id));
        Assert.Equal(50, progresso.Percentual);
        Assert.Equal("Primeiro trecho.", progresso.Transcricao);
        await using (var consulta = banco.Novo())
        {
            var conversa = await consulta.Conversas.SingleAsync(x => x.Id == id);
            Assert.Equal(StatusConversa.Transcrevendo, conversa.Status);
            Assert.Null(conversa.Transcricao);
        }

        if (cancelar)
        {
            cts.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => processamento);
        }
        else
        {
            transcription.Continuar.SetResult();
            await processamento;
        }
        Assert.Null(store.Obter(id));
        await using var resultado = banco.Novo();
        var final = await resultado.Conversas.SingleAsync(x => x.Id == id);
        Assert.Equal(cancelar ? StatusConversa.Transcrevendo : StatusConversa.Concluida, final.Status);
        Assert.Equal(cancelar ? null : "Primeiro trecho. Segundo trecho.", final.Transcricao);
    }

    private sealed class TranscricaoEmAndamento : ITranscriptionService
    {
        public TaskCompletionSource Iniciou { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Continuar { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<string> TranscreverAsync(string caminhoAudio, CancellationToken ct) => throw new NotSupportedException();

        public async Task<string> TranscreverAsync(string caminhoAudio, Action<TranscriptionProgress> progresso, CancellationToken ct)
        {
            progresso(new("Transcrevendo áudio", 30, 60, "Primeiro trecho."));
            Iniciou.SetResult();
            await Continuar.Task.WaitAsync(ct);
            return "Primeiro trecho. Segundo trecho.";
        }
    }
}
