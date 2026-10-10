namespace MemoriaRelacionamento.Models;

public enum StatusConversa { NaFila, Transcrevendo, Analisando, Concluida, Erro }
public enum OrigemConversa { Audio, Texto }
public enum Sentimento { Satisfeito, Alinhado, Neutro, NecessitaAtencao }
public enum CategoriaInsight { Familia, Hobby, Objetivo, Evento, Profissional, Outro }
public enum StatusRevisao { Pendente, Aprovado, Descartado }

public class Cliente
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = "";
    public string? Telefone { get; set; }
    public string? Email { get; set; }
    public DateTimeOffset CriadoEm { get; set; }
    public List<Conversa> Conversas { get; set; } = new();
}

public class Conversa
{
    public Guid Id { get; set; }
    public Guid ClienteId { get; set; }
    public Cliente? Cliente { get; set; }
    public DateOnly Data { get; set; }
    public OrigemConversa Origem { get; set; }
    public string? CaminhoDoAudio { get; set; }
    public StatusConversa Status { get; set; } = StatusConversa.NaFila;
    public string? Transcricao { get; set; }
    public List<string>? Resumo { get; set; }
    public Sentimento? Sentimento { get; set; }
    public List<string>? PautasSugeridas { get; set; }
    public string? MensagemDeErro { get; set; }
    public DateTimeOffset CriadoEm { get; set; }
}

public class InformacaoInsight
{
    public Guid Id { get; set; }
    public Guid ClienteId { get; set; }
    public Guid ConversaId { get; set; }
    public CategoriaInsight Categoria { get; set; }
    public string Conteudo { get; set; } = "";
    public string? TrechoDeOrigem { get; set; }
    public DateOnly? DataReferencia { get; set; }
    public bool RecorrenteAnual { get; set; }
    public StatusRevisao StatusDeRevisao { get; set; } = StatusRevisao.Pendente;
}

public class AcaoToDo
{
    public Guid Id { get; set; }
    public Guid ClienteId { get; set; }
    public Guid? ConversaId { get; set; }
    public string Descricao { get; set; } = "";
    public DateOnly? DataOpcional { get; set; }
    public bool Concluida { get; set; }
}
