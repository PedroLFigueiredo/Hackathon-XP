using MemoriaRelacionamento.Models;

namespace MemoriaRelacionamento.Pages;

/// <summary>Textos de tela para os valores gravados no banco.</summary>
public static class Rotulos
{
    public static readonly string[] GruposPessoais = ["Família", "Hobbies e estilo de vida", "Vida profissional", "Objetivos de vida", "Outras informações"];

    public static string Tom(Sentimento? tom) => tom switch
    {
        Sentimento.Satisfeito => "Satisfeito",
        Sentimento.Alinhado => "Alinhado",
        Sentimento.Neutro => "Neutro",
        Sentimento.NecessitaAtencao => "Requer atenção",
        _ => "",
    };

    public static string ClasseTom(Sentimento? tom) => tom switch
    {
        Sentimento.NecessitaAtencao => "selo-atencao",
        Sentimento.Neutro => "selo-neutro",
        _ => "selo-positivo",
    };

    // Mesmos textos de wwwroot/js/conversa-status.js, que atualiza o selo durante o processamento.
    public static string Status(StatusConversa status) => status switch
    {
        StatusConversa.NaFila => "Na fila",
        StatusConversa.Transcrevendo => "Transcrevendo áudio…",
        StatusConversa.Analisando => "Analisando…",
        StatusConversa.Concluida => "Concluída",
        _ => "Erro",
    };

    public static string Origem(OrigemConversa origem) => origem == OrigemConversa.Audio ? "Áudio" : "Texto";

    /// <summary>A primeira tag de cada item é a categoria gravada pela análise.</summary>
    public static string GrupoPessoal(string[]? tags) => Primeira(tags) switch
    {
        "familia" => GruposPessoais[0],
        "hobby" => GruposPessoais[1],
        "profissional" => GruposPessoais[2],
        "objetivo" => GruposPessoais[3],
        _ => GruposPessoais[4],
    };

    public static string GrupoFinanceiro(string[]? tags) => Primeira(tags) switch
    {
        "objetivo" => "Objetivo",
        "patrimonio" => "Patrimônio",
        "renda" => "Renda",
        "liquidez" => "Liquidez",
        "preocupacao" => "Preocupação",
        _ => "Informação",
    };

    /// <summary>Tags além da categoria, prontas para exibir.</summary>
    public static IEnumerable<string> Etiquetas(string[]? tags, bool incluirPrimeira = false) =>
        (tags ?? []).Skip(incluirPrimeira ? 0 : 1).Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Replace('_', ' '));

    public static bool Anual(string[]? tags) => (tags ?? []).Contains("anual", StringComparer.OrdinalIgnoreCase);

    /// <summary>Para datas anuais, a próxima vez em que a data acontece; para as demais, a própria data.</summary>
    public static DateOnly ProximaOcorrencia(DateOnly data, bool anual, DateOnly hoje)
    {
        if (!anual || data >= hoje) return data;
        var noAno = NoAno(data, hoje.Year);
        return noAno >= hoje ? noAno : NoAno(data, hoje.Year + 1);
    }

    public static string Distancia(DateOnly data, DateOnly hoje) => (data.DayNumber - hoje.DayNumber) switch
    {
        0 => "hoje",
        1 => "amanhã",
        -1 => "ontem",
        > 1 and var dias => $"em {dias} dias",
        var dias => $"há {-dias} dias",
    };

    private static string Primeira(string[]? tags) => tags is { Length: > 0 } ? tags[0].ToLowerInvariant() : "";

    // 29 de fevereiro cai em 28 nos anos que não são bissextos.
    private static DateOnly NoAno(DateOnly data, int ano) => new(ano, data.Month, Math.Min(data.Day, DateTime.DaysInMonth(ano, data.Month)));
}
