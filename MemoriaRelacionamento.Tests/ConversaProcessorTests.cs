using MemoriaRelacionamento.Models;
using Microsoft.EntityFrameworkCore;

namespace MemoriaRelacionamento.Tests;

public class ConversaProcessorTests : IDisposable
{
    private readonly BancoDeTeste _banco = new();
    private readonly TranscricaoStub _transcricao = new();
    private readonly AnaliseStub _analise = new();

    public void Dispose() => _banco.Dispose();

    private async Task ProcessarAsync(Guid id)
    {
        await using var db = _banco.Novo();
        await Fabrica.Processor(db, _transcricao, _analise).ProcessarAsync(id, CancellationToken.None);
    }

    [Fact]
    public async Task Texto_vai_a_Concluida_sem_transcrever()
    {
        var cli = await _banco.CriarClienteAsync();
        var id = await Fabrica.ConversaAsync(_banco, cli.Id, OrigemConversa.Texto, "transcrição já pronta com bastante texto");

        await ProcessarAsync(id);

        await using var db = _banco.Novo();
        var c = await db.Conversas.SingleAsync(x => x.Id == id);
        Assert.Equal(StatusConversa.Concluida, c.Status);
        Assert.Equal(0, _transcricao.Chamadas);
        Assert.Equal(1, _analise.Chamadas);
        Assert.Equal(3, c.Resumo!.Count);
        Assert.Equal(Sentimento.Satisfeito, c.Sentimento);
        Assert.Single(c.PautasSugeridas!);
    }

    [Fact]
    public async Task Resultado_gera_insights_pendentes_e_todos_abertos()
    {
        var cli = await _banco.CriarClienteAsync();
        var id = await Fabrica.ConversaAsync(_banco, cli.Id, OrigemConversa.Texto, "texto");

        await ProcessarAsync(id);

        await using var db = _banco.Novo();
        var insight = await db.Insights.SingleAsync();
        Assert.Equal(StatusRevisao.Pendente, insight.StatusDeRevisao);
        Assert.Equal(CategoriaInsight.Familia, insight.Categoria);
        Assert.Equal(cli.Id, insight.ClienteId);
        var todo = await db.Acoes.SingleAsync();
        Assert.False(todo.Concluida);
        Assert.Equal(new DateOnly(2026, 10, 20), todo.DataOpcional);
    }

    [Fact]
    public async Task Audio_transcreve_grava_transcricao_e_conclui()
    {
        var arquivo = Path.GetTempFileName();
        try
        {
            var cli = await _banco.CriarClienteAsync();
            var id = await Fabrica.ConversaAsync(_banco, cli.Id, OrigemConversa.Audio, null, arquivo);

            await ProcessarAsync(id);

            await using var db = _banco.Novo();
            var c = await db.Conversas.SingleAsync(x => x.Id == id);
            Assert.Equal(StatusConversa.Concluida, c.Status);
            Assert.Equal("texto transcrito do áudio", c.Transcricao);
            Assert.Equal(1, _transcricao.Chamadas);
            Assert.Equal("texto transcrito do áudio", _analise.Ultima!.Transcricao);
        }
        finally { File.Delete(arquivo); }
    }

    [Fact]
    public async Task Falha_na_transcricao_vira_Erro_com_mensagem()
    {
        var arquivo = Path.GetTempFileName();
        try
        {
            _transcricao.Falha = new InvalidOperationException("arquivo corrompido");
            var cli = await _banco.CriarClienteAsync();
            var id = await Fabrica.ConversaAsync(_banco, cli.Id, OrigemConversa.Audio, null, arquivo);

            await ProcessarAsync(id); // não deve lançar

            await using var db = _banco.Novo();
            var c = await db.Conversas.SingleAsync(x => x.Id == id);
            Assert.Equal(StatusConversa.Erro, c.Status);
            Assert.Contains("arquivo corrompido", c.MensagemDeErro);
            Assert.Equal(0, _analise.Chamadas);
        }
        finally { File.Delete(arquivo); }
    }

    [Fact]
    public async Task Falha_na_analise_vira_Erro_e_preserva_a_transcricao()
    {
        var arquivo = Path.GetTempFileName();
        try
        {
            _analise.Falha = new HttpRequestException("sem rede");
            var cli = await _banco.CriarClienteAsync();
            var id = await Fabrica.ConversaAsync(_banco, cli.Id, OrigemConversa.Audio, null, arquivo);

            await ProcessarAsync(id);

            await using var db = _banco.Novo();
            var c = await db.Conversas.SingleAsync(x => x.Id == id);
            Assert.Equal(StatusConversa.Erro, c.Status);
            Assert.Equal("sem rede", c.MensagemDeErro);
            Assert.Equal("texto transcrito do áudio", c.Transcricao);

            // Reprocessar não roda o Whisper de novo.
            _analise.Falha = null;
            await ProcessarAsync(id);
            Assert.Equal(1, _transcricao.Chamadas);
            await using var db2 = _banco.Novo();
            Assert.Equal(StatusConversa.Concluida, (await db2.Conversas.SingleAsync(x => x.Id == id)).Status);
        }
        finally { File.Delete(arquivo); }
    }

    [Fact]
    public async Task Audio_inexistente_vira_Erro()
    {
        var cli = await _banco.CriarClienteAsync();
        var id = await Fabrica.ConversaAsync(_banco, cli.Id, OrigemConversa.Audio, null, @"C:\nao\existe.mp3");

        await ProcessarAsync(id);

        await using var db = _banco.Novo();
        var c = await db.Conversas.SingleAsync(x => x.Id == id);
        Assert.Equal(StatusConversa.Erro, c.Status);
        Assert.NotNull(c.MensagemDeErro);
    }

    [Fact]
    public async Task Sem_transcricao_e_sem_audio_vira_Erro()
    {
        var cli = await _banco.CriarClienteAsync();
        var id = await Fabrica.ConversaAsync(_banco, cli.Id, OrigemConversa.Texto, null);

        await ProcessarAsync(id);

        await using var db = _banco.Novo();
        Assert.Equal(StatusConversa.Erro, (await db.Conversas.SingleAsync(x => x.Id == id)).Status);
    }

    [Fact]
    public async Task Transcricao_vazia_do_whisper_vira_Erro()
    {
        var arquivo = Path.GetTempFileName();
        try
        {
            _transcricao.Texto = "   ";
            var cli = await _banco.CriarClienteAsync();
            var id = await Fabrica.ConversaAsync(_banco, cli.Id, OrigemConversa.Audio, null, arquivo);

            await ProcessarAsync(id);

            await using var db = _banco.Novo();
            Assert.Equal(StatusConversa.Erro, (await db.Conversas.SingleAsync(x => x.Id == id)).Status);
        }
        finally { File.Delete(arquivo); }
    }

    [Fact]
    public async Task Fatos_aprovados_do_cliente_vao_para_a_analise()
    {
        var cli = await _banco.CriarClienteAsync();
        var anterior = await Fabrica.ConversaAsync(_banco, cli.Id, OrigemConversa.Texto, "antiga");
        await using (var db = _banco.Novo())
        {
            db.Insights.AddRange(
                new InformacaoInsight { Id = Guid.NewGuid(), ClienteId = cli.Id, ConversaId = anterior, Conteudo = "Gosta de golfe", StatusDeRevisao = StatusRevisao.Aprovado },
                new InformacaoInsight { Id = Guid.NewGuid(), ClienteId = cli.Id, ConversaId = anterior, Conteudo = "Fato pendente", StatusDeRevisao = StatusRevisao.Pendente },
                new InformacaoInsight { Id = Guid.NewGuid(), ClienteId = cli.Id, ConversaId = anterior, Conteudo = "Fato descartado", StatusDeRevisao = StatusRevisao.Descartado });
            await db.SaveChangesAsync();
        }
        var nova = await Fabrica.ConversaAsync(_banco, cli.Id, OrigemConversa.Texto, "nova conversa");

        await ProcessarAsync(nova);

        Assert.Equal(new[] { "Gosta de golfe" }, _analise.Ultima!.FatosJaConhecidos);
        Assert.Equal("Roberto", _analise.Ultima.NomeCliente);
    }

    [Fact]
    public async Task Reprocessar_nao_duplica_e_preserva_aprovados_e_concluidos()
    {
        var cli = await _banco.CriarClienteAsync();
        var id = await Fabrica.ConversaAsync(_banco, cli.Id, OrigemConversa.Texto, "texto");
        await ProcessarAsync(id);

        await using (var db = _banco.Novo())
        {
            (await db.Insights.SingleAsync()).StatusDeRevisao = StatusRevisao.Aprovado;
            (await db.Acoes.SingleAsync()).Concluida = true;
            await db.SaveChangesAsync();
        }

        await ProcessarAsync(id);

        await using var db2 = _banco.Novo();
        // 1 aprovado antigo + 1 novo pendente; 1 to-do concluído + 1 novo aberto.
        Assert.Equal(2, await db2.Insights.CountAsync());
        Assert.Equal(1, await db2.Insights.CountAsync(i => i.StatusDeRevisao == StatusRevisao.Aprovado));
        Assert.Equal(1, await db2.Insights.CountAsync(i => i.StatusDeRevisao == StatusRevisao.Pendente));
        Assert.Equal(2, await db2.Acoes.CountAsync());
        Assert.Equal(1, await db2.Acoes.CountAsync(a => a.Concluida));
    }

    [Fact]
    public async Task Reprocessar_pendentes_nao_acumula()
    {
        var cli = await _banco.CriarClienteAsync();
        var id = await Fabrica.ConversaAsync(_banco, cli.Id, OrigemConversa.Texto, "texto");

        await ProcessarAsync(id);
        await ProcessarAsync(id);

        await using var db = _banco.Novo();
        Assert.Equal(1, await db.Insights.CountAsync());
        Assert.Equal(1, await db.Acoes.CountAsync());
    }

    [Fact]
    public async Task Conversa_inexistente_nao_lanca()
    {
        await ProcessarAsync(Guid.NewGuid());
    }

    [Fact]
    public async Task Cancelamento_propaga_e_nao_marca_Erro()
    {
        var cli = await _banco.CriarClienteAsync();
        var id = await Fabrica.ConversaAsync(_banco, cli.Id, OrigemConversa.Texto, "texto");
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await using var db = _banco.Novo();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            Fabrica.Processor(db, _transcricao, _analise).ProcessarAsync(id, cts.Token));

        await using var db2 = _banco.Novo();
        Assert.NotEqual(StatusConversa.Erro, (await db2.Conversas.SingleAsync(x => x.Id == id)).Status);
    }
}
