using MemoriaRelacionamento.Models;
using MemoriaRelacionamento.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace MemoriaRelacionamento.Tests;

public class ConversaServiceTests : IDisposable
{
    private readonly BancoDeTeste _banco = new();
    private readonly ConversaQueue _fila = new();
    private readonly string _uploads = Path.Combine(Path.GetTempPath(), "uploads-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        _banco.Dispose();
        if (Directory.Exists(_uploads)) Directory.Delete(_uploads, true);
    }

    private ConversaService Servico(Data.AppDbContext db)
    {
        var cfg = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Uploads:Path"] = _uploads }).Build();
        return new ConversaService(db, _fila, cfg, new AmbienteFake());
    }

    private static async Task<Guid> LerComTimeoutAsync(ConversaQueue fila)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        return await fila.LerAsync(cts.Token);
    }

    [Fact]
    public async Task Texto_curto_e_rejeitado()
    {
        var cli = await _banco.CriarClienteAsync();
        await using var db = _banco.Novo();
        await Assert.ThrowsAsync<ArgumentException>(() =>
            Servico(db).CriarDeTextoAsync(cli.Id, new DateOnly(2026, 10, 10), "curto", default));
    }

    [Fact]
    public async Task Texto_cria_conversa_NaFila_e_enfileira()
    {
        var cli = await _banco.CriarClienteAsync();
        var texto = new string('a', ConversaService.TamanhoMinimoTexto);
        await using var db = _banco.Novo();

        var id = await Servico(db).CriarDeTextoAsync(cli.Id, new DateOnly(2026, 10, 10), "  " + texto + "  ", default);

        await using var db2 = _banco.Novo();
        var c = await db2.Conversas.SingleAsync(x => x.Id == id);
        Assert.Equal(OrigemConversa.Texto, c.Origem);
        Assert.Equal(StatusConversa.NaFila, c.Status);
        Assert.Equal(texto, c.Transcricao);
        Assert.Null(c.CaminhoDoAudio);
        Assert.Equal(id, await LerComTimeoutAsync(_fila));
    }

    [Theory]
    [InlineData("reuniao.txt")]
    [InlineData("reuniao.exe")]
    [InlineData("semextensao")]
    public async Task Audio_com_extensao_invalida_e_rejeitado(string nome)
    {
        var cli = await _banco.CriarClienteAsync();
        await using var db = _banco.Novo();
        await Assert.ThrowsAsync<ArgumentException>(() =>
            Servico(db).CriarDeAudioAsync(cli.Id, new DateOnly(2026, 10, 10), new MemoryStream(new byte[10]), nome, default));
        Assert.False(Directory.Exists(_uploads) && Directory.GetFiles(_uploads).Length > 0);
    }

    [Theory]
    [InlineData("Reuniao.MP3", ".mp3")]
    [InlineData("a.wav", ".wav")]
    [InlineData("b.m4a", ".m4a")]
    public async Task Audio_valido_e_salvo_em_uploads_e_enfileirado(string nome, string ext)
    {
        var cli = await _banco.CriarClienteAsync();
        await using var db = _banco.Novo();

        var id = await Servico(db).CriarDeAudioAsync(cli.Id, new DateOnly(2026, 10, 10),
            new MemoryStream(new byte[] { 1, 2, 3 }), nome, default);

        await using var db2 = _banco.Novo();
        var c = await db2.Conversas.SingleAsync(x => x.Id == id);
        Assert.Equal(OrigemConversa.Audio, c.Origem);
        Assert.Equal(StatusConversa.NaFila, c.Status);
        Assert.Null(c.Transcricao);
        Assert.Equal(Path.Combine(_uploads, id + ext), c.CaminhoDoAudio);
        Assert.Equal(3, new FileInfo(c.CaminhoDoAudio!).Length);
        Assert.Equal(id, await LerComTimeoutAsync(_fila));
    }

    private sealed class AmbienteFake : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Test";
        public string ApplicationName { get; set; } = "Test";
        public string ContentRootPath { get; set; } = Path.GetTempPath();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}

public class ConversaQueueTests
{
    [Fact]
    public async Task Mantem_a_ordem_de_chegada()
    {
        var fila = new ConversaQueue();
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        fila.Enfileirar(a);
        fila.Enfileirar(b);

        Assert.Equal(a, await fila.LerAsync(default));
        Assert.Equal(b, await fila.LerAsync(default));
    }

    [Fact]
    public async Task Leitura_respeita_cancelamento()
    {
        var fila = new ConversaQueue();
        using var cts = new CancellationTokenSource(50);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await fila.LerAsync(cts.Token));
    }
}
