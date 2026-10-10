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
    }

    [Fact]
    public async Task Cada_tipo_de_informacao_vai_para_a_sua_tabela_com_tags()
    {
        _analise.Resultado = _analise.Resultado with
        {
            Eventos = new() { new("Aniversário da Marina", new DateOnly(2026, 11, 15), "faz quinze em novembro", new() { "anual", "familia" }) },
            Financeiro = new() { new("Quer comprar um apartamento na praia", "o apê na praia", new() { "objetivo", "imovel" }) },
            PautasSugeridas = new() { "Perguntar da viagem" },
        };
        var cli = await _banco.CriarClienteAsync();
        var id = await Fabrica.ConversaAsync(_banco, cli.Id, OrigemConversa.Texto, "texto");

        await ProcessarAsync(id);

        await using var db = _banco.Novo();
        Assert.Equal(new[] { "familia", "filha" }, (await db.Insights.SingleAsync()).Tags);
        var evento = await db.Eventos.SingleAsync();
        Assert.Equal(new DateOnly(2026, 11, 15), evento.DataEvento);
        Assert.Equal(new[] { "anual", "familia" }, evento.Tags);
        var financeiro = await db.Financeiro.SingleAsync();
        Assert.Equal("o apê na praia", financeiro.TrechoOrigem);
        Assert.Equal(new[] { "objetivo", "imovel" }, financeiro.Tags);
        var acoes = await db.ToDos.OrderBy(x => x.Descricao).ToListAsync();
        Assert.Equal(new[] { "tarefa", "previdencia" }, acoes.Single(x => x.Descricao == "Enviar simulação").Tags);
        // O schema não tem coluna de pautas: elas entram em acoes com a tag reservada.
        var pauta = acoes.Single(x => x.Descricao == "Perguntar da viagem");
        Assert.Equal(new[] { "pauta" }, pauta.Tags);
        Assert.Equal(id, pauta.ConversaId);
        Assert.False(pauta.Concluida);
    }

    [Fact]
    public async Task Insights_do_assessor_sao_gravados_por_conversa_e_o_perfil_cadastrado_vai_na_analise()
    {
        _analise.Resultado = _analise.Resultado with
        {
            InsightsAssessor = new() { new(TiposDeInsight.ApetiteRisco, "Quer tomar mais risco", "Revisar o perfil com o cliente", "quero arriscar mais") },
        };
        var cli = await _banco.CriarClienteAsync();
        await using (var db = _banco.Novo())
        {
            (await db.Clientes.SingleAsync(x => x.Id == cli.Id)).PerfilFinanceiro = "Conservador";
            await db.SaveChangesAsync();
        }
        var primeira = await Fabrica.ConversaAsync(_banco, cli.Id, OrigemConversa.Texto, "texto");
        var segunda = await Fabrica.ConversaAsync(_banco, cli.Id, OrigemConversa.Texto, "outro texto");

        await ProcessarAsync(primeira);
        await ProcessarAsync(segunda);

        Assert.Equal("Conservador", _analise.Ultima!.PerfilFinanceiroCadastrado);
        await using var db2 = _banco.Novo();
        // Sinais agregam: cada conversa acrescenta os seus, sem apagar os anteriores.
        var insights = await db2.InsightsAssessor.ToListAsync();
        Assert.Equal(new[] { primeira, segunda }.Order(), insights.Select(x => x.ConversaId).Order());
        Assert.All(insights, x =>
        {
            Assert.Equal(cli.Id, x.ClienteId);
            Assert.Equal("apetite_risco", x.Tipo);
            Assert.Equal("Revisar o perfil com o cliente", x.Sugestao);
            Assert.Equal("quero arriscar mais", x.TrechoOrigem);
        });
    }

    [Fact]
    public async Task Personalidade_e_atualizada_e_perfil_financeiro_so_preenche_quando_vazio()
    {
        _analise.Resultado = _analise.Resultado with { Personalidade = "Analítico", PerfilFinanceiro = "Arrojado" };
        var semPerfil = await _banco.CriarClienteAsync("Sem perfil");
        var comPerfil = await _banco.CriarClienteAsync("Com perfil");
        await using (var db = _banco.Novo())
        {
            var cadastrado = await db.Clientes.SingleAsync(x => x.Id == comPerfil.Id);
            cadastrado.PerfilFinanceiro = "Conservador";
            cadastrado.Personalidade = "Reservado";
            await db.SaveChangesAsync();
        }

        await ProcessarAsync(await Fabrica.ConversaAsync(_banco, semPerfil.Id, OrigemConversa.Texto, "texto"));
        await ProcessarAsync(await Fabrica.ConversaAsync(_banco, comPerfil.Id, OrigemConversa.Texto, "texto"));

        await using var db2 = _banco.Novo();
        var a = await db2.Clientes.SingleAsync(x => x.Id == semPerfil.Id);
        Assert.Equal("Analítico", a.Personalidade);
        Assert.Equal("Arrojado", a.PerfilFinanceiro);
        var b = await db2.Clientes.SingleAsync(x => x.Id == comPerfil.Id);
        Assert.Equal("Analítico", b.Personalidade);
        Assert.Equal("Conservador", b.PerfilFinanceiro); // o perfil do cadastro não é sobrescrito pela conversa
    }

    [Fact]
    public async Task Analise_sem_personalidade_nao_apaga_a_que_ja_existe()
    {
        var cli = await _banco.CriarClienteAsync();
        await using (var db = _banco.Novo())
        {
            (await db.Clientes.SingleAsync(x => x.Id == cli.Id)).Personalidade = "Reservado";
            await db.SaveChangesAsync();
        }

        await ProcessarAsync(await Fabrica.ConversaAsync(_banco, cli.Id, OrigemConversa.Texto, "texto"));

        await using var db2 = _banco.Novo();
        Assert.Equal("Reservado", (await db2.Clientes.SingleAsync(x => x.Id == cli.Id)).Personalidade);
    }

    [Fact]
    public async Task Falha_da_analise_mostra_o_motivo_ao_assessor()
    {
        _analise.Falha = new MemoriaRelacionamento.Services.AnaliseException("A chave da OpenAI foi recusada. Confira OPENAI_API_KEY");
        var cli = await _banco.CriarClienteAsync();
        var id = await Fabrica.ConversaAsync(_banco, cli.Id, OrigemConversa.Texto, "texto");

        await ProcessarAsync(id);

        await using var db = _banco.Novo();
        var c = await db.Conversas.SingleAsync(x => x.Id == id);
        Assert.Equal(StatusConversa.Erro, c.Status);
        Assert.Contains("A chave da OpenAI foi recusada", c.MensagemDeErro);
        Assert.Empty(await db.Insights.ToListAsync());
    }

    [Fact]
    public async Task Resultado_gera_insights_automaticos_e_todos_abertos()
    {
        var cli = await _banco.CriarClienteAsync();
        var id = await Fabrica.ConversaAsync(_banco, cli.Id, OrigemConversa.Texto, "texto");

        await ProcessarAsync(id);

        await using var db = _banco.Novo();
        var insight = await db.Insights.SingleAsync();
        Assert.Equal(CategoriaInsight.Familia, insight.Categoria);
        Assert.Equal(cli.Id, insight.ClienteId);
        var todo = await db.ToDos.SingleAsync();
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
            Assert.NotNull(c.MensagemDeErro);
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
            Assert.NotNull(c.MensagemDeErro);
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
    public async Task Fatos_do_cliente_entram_automaticamente_na_analise()
    {
        var cli = await _banco.CriarClienteAsync();
        var anterior = await Fabrica.ConversaAsync(_banco, cli.Id, OrigemConversa.Texto, "antiga");
        await using (var db = _banco.Novo())
        {
            db.Insights.Add(new InformacaoInsight { ClienteId = cli.Id, ConversaId = anterior, Conteudo = "Gosta de golfe" });
            await db.SaveChangesAsync();
        }
        var nova = await Fabrica.ConversaAsync(_banco, cli.Id, OrigemConversa.Texto, "nova conversa");
        await ProcessarAsync(nova);
        Assert.Equal(new[] { "Gosta de golfe" }, _analise.Ultima!.FatosJaConhecidos);
    }
    [Fact]
    public async Task Conversa_concluida_nao_e_processada_novamente()
    {
        var cli = await _banco.CriarClienteAsync();
        var id = await Fabrica.ConversaAsync(_banco, cli.Id, OrigemConversa.Texto, "texto");
        await ProcessarAsync(id);
        await using (var db = _banco.Novo())
        {
            (await db.ToDos.SingleAsync()).Concluida = true;
            await db.SaveChangesAsync();
        }
        await ProcessarAsync(id);
        await using var db2 = _banco.Novo();
        Assert.Equal(1, await db2.Insights.CountAsync());
        Assert.True((await db2.ToDos.SingleAsync()).Concluida);
        Assert.Equal(1, _analise.Chamadas);
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
        Assert.Equal(1, await db.ToDos.CountAsync());
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
