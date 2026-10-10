using System.Net;
using System.Text;
using System.Text.Json;
using MemoriaRelacionamento.Models;
using MemoriaRelacionamento.Services;
using MemoriaRelacionamento.Services.Fakes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace MemoriaRelacionamento.Tests;

public class OpenAiConversationAnalysisServiceTests
{
    private static readonly string Falas = string.Concat(Enumerable.Repeat("a Marina faz quinze em novembro e eu queria o apê na praia. ", 12));
    private static readonly AnaliseRequest Pedido = new("Roberto", new DateOnly(2026, 10, 10),
        Falas + "Te mando até sexta-feira que vem.", ["Gosta de golfe"]);

    private const string Analise = """
        {
          "resumo": ["Falou da família.", "Quer um imóvel.", "Pediu uma simulação."],
          "tom": "atencao",
          "personalidade": "analitico",
          "perfil_financeiro": null,
          "perfil_pessoal": [{ "categoria": "familia", "conteudo": "Filha Marina", "trecho_origem": "a Marina", "tags": ["Filha", "família"] }],
          "eventos": [{ "categoria": "anual", "conteudo": "Aniversário da Marina", "data_evento": "2026-11-15", "trecho_origem": "faz quinze em novembro", "tags": ["familia"] }],
          "financeiro": [{ "categoria": "objetivo", "conteudo": "Quer um apartamento na praia", "trecho_origem": "o apê na praia", "tags": ["imovel"] }],
          "acoes": [
            { "descricao": "Enviar simulação", "data": "sem data", "dia_da_semana": null, "tags": ["previdencia"] },
            { "descricao": "Mandar opções de renda fixa", "data": "2026-10-14", "dia_da_semana": "sexta", "tags": [] },
            { "descricao": "Ligar de volta", "data": "2026-10-23", "dia_da_semana": "sexta", "tags": [] }
          ],
          "pautas_sugeridas": ["Perguntar do aniversário"],
          "insights_assessor": [
            { "tipo": "oportunidade", "direcao_risco": null, "conteudo": "Planeja comprar um imóvel", "sugestao": "Perguntar prazo e valor", "trecho_origem": "eu queria o apê na praia" },
            { "tipo": "capacidade_financeira", "direcao_risco": "mais_risco", "conteudo": "Tem folga para arriscar", "sugestao": "Confirmar o prazo.", "trecho_origem": "a Marina faz quinze" },
            { "tipo": "alerta", "direcao_risco": null, "conteudo": "Mesmo sinal repetido", "sugestao": "", "trecho_origem": "Eu queria o apê na praia." },
            { "tipo": "apetite_risco", "direcao_risco": "mais_risco", "conteudo": "Quer arriscar mais", "sugestao": "Revisar o perfil", "trecho_origem": "quero dobrar meu risco" },
            { "tipo": "apetite_risco", "direcao_risco": "mais_risco", "conteudo": "Compara com quem investe em ações", "sugestao": "Perguntar quanto de queda ele aceita.", "trecho_origem": "faz quinze em novembro" },
            { "tipo": "tipo_inventado", "direcao_risco": null, "conteudo": "Outro sinal", "sugestao": "", "trecho_origem": "a Marina" }
          ]
        }
        """;

    private sealed class Handler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        public string? Requisicao;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Requisicao = await request.Content!.ReadAsStringAsync(ct);
            return new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
        }
    }

    private static string Resposta(string conteudo, string motivo = "stop", string? recusa = null) => JsonSerializer.Serialize(new
    {
        choices = new[] { new { finish_reason = motivo, message = new { role = "assistant", content = conteudo, refusal = recusa } } },
    });

    private static (OpenAiConversationAnalysisService Servico, Handler Handler) Criar(HttpStatusCode status, string body)
    {
        var handler = new Handler(status, body);
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.openai.com/") };
        return (new(http, new ConfigurationBuilder().Build(), NullLogger<OpenAiConversationAnalysisService>.Instance), handler);
    }

    [Fact]
    public async Task Converte_o_json_para_o_formato_das_tabelas()
    {
        var (servico, _) = Criar(HttpStatusCode.OK, Resposta(Analise));

        var r = await servico.AnalisarAsync(Pedido, default);

        Assert.Equal(3, r.Resumo.Count);
        Assert.Equal(Sentimento.NecessitaAtencao, r.Sentimento);
        Assert.Equal("Analítico", r.Personalidade);
        Assert.Null(r.PerfilFinanceiro);
        // A categoria vira a primeira tag; as demais ficam em minúsculas, sem acento e sem repetição.
        Assert.Equal(new[] { "familia", "filha" }, r.PerfilPessoal.Single().Tags);
        Assert.Equal("a Marina", r.PerfilPessoal.Single().TrechoOrigem);
        Assert.Equal(new DateOnly(2026, 11, 15), r.Eventos.Single().DataEvento);
        Assert.Equal(new[] { "anual", "familia" }, r.Eventos.Single().Tags);
        Assert.Equal(new[] { "objetivo", "imovel" }, r.Financeiro.Single().Tags);
        Assert.Null(r.Acoes[0].Data); // data fora do formato é descartada, a ação fica
        // 14/10/2026 é quarta: o dia da semana dito vale mais que a conta do modelo (reunião no sábado, 10/10).
        Assert.Equal(new DateOnly(2026, 10, 16), r.Acoes[1].Data);
        Assert.Equal(new DateOnly(2026, 10, 23), r.Acoes[2].Data); // data que já cai numa sexta é mantida
        Assert.Equal(new[] { "Perguntar do aniversário" }, r.PautasSugeridas);
    }

    [Fact]
    public async Task Insight_so_chega_ao_assessor_com_fala_do_cliente_que_o_sustente()
    {
        var (servico, handler) = Criar(HttpStatusCode.OK, Resposta(Analise));

        var r = await servico.AnalisarAsync(Pedido with { PerfilFinanceiroCadastrado = "Conservador" }, default);

        // Ficam de fora: a fala que não está na transcrição, o tipo que não existe e o sinal repetido sobre a mesma fala.
        Assert.Equal(3, r.InsightsAssessor.Count);
        var oportunidade = r.InsightsAssessor[0];
        Assert.Equal(TiposDeInsight.Oportunidade, oportunidade.Tipo);
        Assert.Equal("Perguntar prazo e valor", oportunidade.Sugestao);
        Assert.Equal("eu queria o apê na praia", oportunidade.TrechoOrigem);
        // Sinal de mais risco com cadastro Conservador: o aviso de revisar o perfil vem do código, antes da
        // sugestão, em qualquer tipo de sinal e uma vez só por conversa.
        var capacidade = r.InsightsAssessor[1];
        Assert.Equal(TiposDeInsight.CapacidadeFinanceira, capacidade.Tipo);
        Assert.StartsWith("O perfil cadastrado é Conservador e a conversa aponta para mais risco", capacidade.Sugestao);
        Assert.EndsWith("Confirmar o prazo.", capacidade.Sugestao);
        var risco = r.InsightsAssessor[2];
        Assert.Equal(TiposDeInsight.ApetiteRisco, risco.Tipo);
        Assert.Equal("Perguntar quanto de queda ele aceita.", risco.Sugestao);
        using var enviado = JsonDocument.Parse(handler.Requisicao!);
        Assert.Contains("Perfil de investidor cadastrado: Conservador", enviado.RootElement.GetProperty("messages")[0].GetProperty("content").GetString());
    }

    [Theory]
    [InlineData("Como eu te falei, meu perfil é moderado e não quero mudar.", "moderado", "Moderado")]
    [InlineData("Mantemos então o perfil de investidor moderado.", "moderado", "Moderado")]
    [InlineData("Eu sou um investidor agressivo, você sabe disso.", "arrojado", "Arrojado")] // "agressivo" é o arrojado
    [InlineData("Sempre fui uma investidora bem conservadora.", "conservador", "Conservador")]
    [InlineData("Quero colocar uma parte em coisa mais arrojada, mas com risco moderado.", "moderado", null)] // deduzido, não dito
    [InlineData("Quero ações agressivas de tecnologia.", "arrojado", null)] // fala de produto, não do perfil dele
    [InlineData("Meu perfil sempre foi conservador.", "moderado", null)] // o modelo respondeu outro perfil
    public async Task Perfil_financeiro_so_vale_quando_foi_dito_na_conversa(string fala, string modelo, string? esperado)
    {
        var (servico, _) = Criar(HttpStatusCode.OK, Resposta(Analise.Replace("\"perfil_financeiro\": null", $"\"perfil_financeiro\": \"{modelo}\"")));

        var r = await servico.AnalisarAsync(Pedido with { Transcricao = Falas + fala }, default);

        Assert.Equal(esperado, r.PerfilFinanceiro);
    }

    [Fact]
    public async Task Sem_cadastro_a_divergencia_usa_o_perfil_que_o_cliente_declarou_na_conversa()
    {
        var analise = Analise
            .Replace("\"perfil_financeiro\": null", "\"perfil_financeiro\": \"arrojado\"")
            .Replace("\"tipo\": \"capacidade_financeira\", \"direcao_risco\": \"mais_risco\"", "\"tipo\": \"capacidade_financeira\", \"direcao_risco\": \"menos_risco\"");
        var (servico, _) = Criar(HttpStatusCode.OK, Resposta(analise));

        var r = await servico.AnalisarAsync(Pedido with { Transcricao = Falas + "Eu sou um investidor agressivo." }, default);

        Assert.Equal("Arrojado", r.PerfilFinanceiro);
        var sinal = r.InsightsAssessor.Single(x => x.Tipo == TiposDeInsight.CapacidadeFinanceira);
        Assert.StartsWith("O cliente se declara Arrojado, mas a conversa aponta para menos risco", sinal.Sugestao);
    }

    [Theory]
    [InlineData("Arrojado")] // mais risco não contradiz um cadastro arrojado
    [InlineData(null)]       // sem perfil cadastrado não há com o que comparar
    public async Task Sem_divergencia_do_cadastro_a_sugestao_do_modelo_fica_como_veio(string? perfil)
    {
        var (servico, _) = Criar(HttpStatusCode.OK, Resposta(Analise));

        var r = await servico.AnalisarAsync(Pedido with { PerfilFinanceiroCadastrado = perfil }, default);

        Assert.Equal("Perguntar quanto de queda ele aceita.", r.InsightsAssessor.Single(x => x.Tipo == TiposDeInsight.ApetiteRisco).Sugestao);
    }

    [Fact]
    public async Task Citacao_que_nao_esta_na_transcricao_e_descartada_mas_o_fato_fica()
    {
        var inventada = Analise.Replace("\"trecho_origem\": \"a Marina\"", "\"trecho_origem\": \"minha filha querida\"");
        var (servico, _) = Criar(HttpStatusCode.OK, Resposta(inventada));

        var r = await servico.AnalisarAsync(Pedido, default);

        Assert.Equal("Filha Marina", r.PerfilPessoal.Single().Conteudo);
        Assert.Equal("", r.PerfilPessoal.Single().TrechoOrigem);
        // Diferença só de pontuação, acento ou maiúsculas não descarta a citação.
        Assert.Equal("faz quinze em novembro", r.Eventos.Single().TrechoOrigem);
    }

    [Fact]
    public async Task Dia_da_semana_que_nao_foi_dito_nao_altera_a_data()
    {
        var (servico, _) = Criar(HttpStatusCode.OK, Resposta(Analise));

        var r = await servico.AnalisarAsync(Pedido with { Transcricao = Falas }, default);

        Assert.Equal(new DateOnly(2026, 10, 14), r.Acoes[1].Data);
    }

    [Fact]
    public async Task Conversa_curta_nao_estima_personalidade()
    {
        var (servico, _) = Criar(HttpStatusCode.OK, Resposta(Analise));

        var r = await servico.AnalisarAsync(Pedido with { Transcricao = "A Marina mandou um abraço. Pode renovar o CDB, por favor." }, default);

        Assert.Null(r.Personalidade);
        Assert.Single(r.PerfilPessoal); // o resto da análise é mantido
    }

    [Fact]
    public async Task Envia_schema_estrito_contexto_e_fatos_conhecidos()
    {
        var (servico, handler) = Criar(HttpStatusCode.OK, Resposta(Analise));

        await servico.AnalisarAsync(Pedido, default);

        using var enviado = JsonDocument.Parse(handler.Requisicao!);
        var raiz = enviado.RootElement;
        Assert.Equal(OpenAiConversationAnalysisService.ModeloPadrao, raiz.GetProperty("model").GetString());
        var formato = raiz.GetProperty("response_format");
        Assert.Equal("json_schema", formato.GetProperty("type").GetString());
        Assert.True(formato.GetProperty("json_schema").GetProperty("strict").GetBoolean());
        var instrucoes = raiz.GetProperty("messages")[0].GetProperty("content").GetString()!;
        Assert.Contains("Roberto", instrucoes);
        Assert.Contains("2026-10-10", instrucoes);
        Assert.Contains("Gosta de golfe", instrucoes);
        Assert.Contains("sexta-feira = 2026-10-16", instrucoes); // calendário para resolver "sexta que vem"
        Assert.Equal(Pedido.Transcricao, raiz.GetProperty("messages")[1].GetProperty("content").GetString());
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, "chave da OpenAI foi recusada")]
    [InlineData(HttpStatusCode.TooManyRequests, "Limite de uso")]
    [InlineData(HttpStatusCode.BadRequest, "modelo inexistente")]
    public async Task Erro_http_vira_mensagem_legivel(HttpStatusCode status, string esperado)
    {
        var (servico, _) = Criar(status, """{ "error": { "message": "modelo inexistente" } }""");

        var erro = await Assert.ThrowsAsync<AnaliseException>(() => servico.AnalisarAsync(Pedido, default));

        Assert.Contains(esperado, erro.Message);
    }

    [Theory]
    [InlineData("length", null, "cortada")]
    [InlineData("stop", "não posso ajudar", "recusou")]
    public async Task Resposta_cortada_ou_recusada_nao_vira_analise(string motivo, string? recusa, string esperado)
    {
        var (servico, _) = Criar(HttpStatusCode.OK, Resposta(Analise, motivo, recusa));

        var erro = await Assert.ThrowsAsync<AnaliseException>(() => servico.AnalisarAsync(Pedido, default));

        Assert.Contains(esperado, erro.Message);
    }

    [Fact]
    public async Task Conteudo_fora_do_formato_vira_erro_de_analise()
    {
        var (servico, _) = Criar(HttpStatusCode.OK, Resposta("isto não é json"));

        await Assert.ThrowsAsync<AnaliseException>(() => servico.AnalisarAsync(Pedido, default));
    }

    [Theory]
    [InlineData(null, null, typeof(TranscriptOnlyAnalysisService))]
    [InlineData(null, "sk-teste", typeof(OpenAiConversationAnalysisService))]
    [InlineData("true", "sk-teste", typeof(FakeConversationAnalysisService))]
    public void Registro_escolhe_o_servico_pela_configuracao(string? fake, string? chave, Type esperado)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["Fakes:Analise"] = fake, ["OPENAI_API_KEY"] = chave }).Build();
        var services = new ServiceCollection().AddLogging().AddSingleton<IConfiguration>(configuration);

        using var provider = services.AddAnalysis(configuration).BuildServiceProvider();
        using var scope = provider.CreateScope();

        Assert.IsType(esperado, scope.ServiceProvider.GetRequiredService<IConversationAnalysisService>());
    }
}
