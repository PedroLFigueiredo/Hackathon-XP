using MemoriaRelacionamento.Services;
using MemoriaRelacionamento.Models;
using Microsoft.EntityFrameworkCore;
namespace MemoriaRelacionamento.Tests;
public class DemoClientesTests
{
    [Fact]
    public void Mapeamento_Postgres_usa_somente_colunas_do_schema_enviado()
    {
        using var db = new MemoriaRelacionamento.Data.AppDbContext(
            new DbContextOptionsBuilder<MemoriaRelacionamento.Data.AppDbContext>().UseNpgsql("Host=localhost;Database=contrato;Username=contrato").Options);
        var expected = new Dictionary<string, string[]>
        {
            ["clientes"] = ["id", "nome", "telefone", "email", "perfil_financeiro", "personalidade"],
            ["conversas"] = ["id", "cliente_id", "data", "caminho_audio", "tom", "status", "transcricao", "resumo", "mensagem_erro"],
            ["perfil_pessoal"] = ["id", "cliente_id", "conversa_id", "conteudo", "trecho_origem", "tags", "status_revisao"],
            ["informacao_eventos"] = ["id", "cliente_id", "conteudo", "data_evento", "trecho_origem", "tags", "status_revisao"],
            ["informacao_financeiro"] = ["id", "cliente_id", "conteudo", "trecho_origem", "tags", "status_revisao"],
            ["acoes"] = ["id", "cliente_id", "conversa_id", "descricao", "data_opcional", "tags", "status_conclusao"],
            // Acrescentada depois do schema enviado, em db/insights_assessor.sql.
            ["insights_assessor"] = ["id", "cliente_id", "conversa_id", "tipo", "conteudo", "sugestao", "trecho_origem"]
        };
        foreach (var entity in db.Model.GetEntityTypes())
            Assert.Equal(expected[entity.GetTableName()!].OrderBy(x => x), entity.GetProperties().Select(x => x.GetColumnName()).OrderBy(x => x));
        Assert.Equal(7, db.Model.GetEntityTypes().Count());
    }

    [Fact]
    public async Task Carga_grava_seis_clientes_sem_duplicar_e_preserva_historico()
    {
        using var banco = new BancoDeTeste();
        await using var db = banco.Novo();
        var service = new DemoClientesService(db);
        await service.CarregarAsync(CancellationToken.None);
        var clientes = await db.Clientes.AsNoTracking().ToListAsync();
        Assert.Equal(6, clientes.Count);
        // A carga traz só o cadastro: perfil financeiro e personalidade vêm da análise das conversas.
        Assert.All(clientes, c => { Assert.Null(c.PerfilFinanceiro); Assert.Null(c.Personalidade); });
        var conversa = new Conversa { ClienteId = clientes[0].Id, Data = new DateOnly(2026, 10, 10), Transcricao = "Histórico preservado" };
        db.Conversas.Add(conversa);
        var analisado = await db.Clientes.SingleAsync(x => x.Id == clientes[0].Id);
        analisado.Personalidade = "Analítico";
        analisado.PerfilFinanceiro = "Moderado";
        await db.SaveChangesAsync();
        await service.CarregarAsync(CancellationToken.None);
        Assert.Equal(6, await db.Clientes.CountAsync());
        // Repetir a carga não apaga o que a análise gravou no cliente.
        var preservado = await db.Clientes.AsNoTracking().SingleAsync(x => x.Id == clientes[0].Id);
        Assert.Equal("Analítico", preservado.Personalidade);
        Assert.Equal("Moderado", preservado.PerfilFinanceiro);
        Assert.Equal("Histórico preservado", (await db.Conversas.SingleAsync()).Transcricao);
    }
    [Fact]
    public async Task Eventos_e_financas_entram_na_analise_e_nas_tabelas_do_schema()
    {
        using var banco = new BancoDeTeste();
        var cliente = await banco.CriarClienteAsync();
        await using var db = banco.Novo();
        db.Eventos.Add(new() { ClienteId = cliente.Id, Conteudo = "Viagem em novembro" });
        db.Financeiro.Add(new() { ClienteId = cliente.Id, Conteudo = "Reserva de emergência" });
        await db.SaveChangesAsync();
        var id = await Fabrica.ConversaAsync(banco, cliente.Id, OrigemConversa.Texto, "Transcrição para análise");
        var analise = new AnaliseStub { Resultado = new(["Resumo"], Sentimento.Neutro, null, null, [],
            [new("Aniversário", new DateOnly(2026, 11, 1), "trecho", ["anual"])], [new("Comprar casa", "trecho", ["objetivo"])], [], []) };
        await Fabrica.Processor(db, new TranscricaoStub(), analise).ProcessarAsync(id, CancellationToken.None);
        Assert.Contains("Viagem em novembro", analise.Ultima!.FatosJaConhecidos);
        Assert.Contains("Reserva de emergência", analise.Ultima.FatosJaConhecidos);
        Assert.Equal(2, await db.Eventos.CountAsync());
        Assert.Equal(2, await db.Financeiro.CountAsync());
        Assert.Empty(await db.Insights.ToListAsync());
        Assert.Equal(StatusConversa.Concluida, (await db.Conversas.SingleAsync()).Status);
    }
}
