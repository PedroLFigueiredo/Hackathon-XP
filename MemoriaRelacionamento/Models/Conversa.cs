namespace MemoriaRelacionamento.Models;

public enum StatusConversa { NaFila, Transcrevendo, Analisando, Concluida, Erro }
public enum OrigemConversa { Audio, Texto }
public enum Sentimento { Satisfeito, Alinhado, Neutro, NecessitaAtencao }
public enum CategoriaInsight { Familia, Hobby, Objetivo, Evento, Profissional, Outro }

public class Conversa
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ClienteId { get; set; }
    public DateTime DataRegistro { get; set; } = DateTime.UtcNow;
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public DateOnly Data { get => DateOnly.FromDateTime(DataRegistro); set => DataRegistro = DateTime.SpecifyKind(value.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc); }
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public OrigemConversa Origem { get => string.IsNullOrEmpty(CaminhoDoAudio) ? OrigemConversa.Texto : OrigemConversa.Audio; set { } }
    public string? CaminhoDoAudio { get; set; } = "";
    public StatusConversa Status { get; set; } = StatusConversa.NaFila;
    public string? Transcricao { get; set; }
    public List<string> Resumo { get; set; } = [];
    public Sentimento? Sentimento { get; set; }
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public List<string> PautasSugeridas { get; set; } = [];
    public string? MensagemDeErro { get; set; }
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}

public class InformacaoInsight
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ClienteId { get; set; }
    public Guid ConversaId { get; set; }
    public string[] Tags { get; set; } = [];
    public string StatusRevisao { get; set; } = "pendente";
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public CategoriaInsight Categoria { get => Tags is { Length: > 0 } && Enum.TryParse<CategoriaInsight>(Tags[0], out var c) ? c : CategoriaInsight.Outro; set => Tags = [value.ToString()]; }
    public string Conteudo { get; set; } = "";
    public string? TrechoDeOrigem { get; set; }
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public DateOnly? DataReferencia { get; set; }
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public bool RecorrenteAnual { get; set; }
}

public class AcaoToDo
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ClienteId { get; set; }
    public Guid? ConversaId { get; set; }
    public string Descricao { get; set; } = "";
    public DateOnly? DataOpcional { get; set; }
    public string[] Tags { get; set; } = [];
    public string StatusConclusao { get; set; } = "pendente";
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public bool Concluida { get => StatusConclusao == "concluido"; set => StatusConclusao = value ? "concluido" : "pendente"; }
}

public class InformacaoEvento {
 public Guid Id { get; set; } = Guid.NewGuid();
 public Guid ClienteId { get; set; }
 public string Conteudo { get; set; } = "";
 public DateOnly? DataEvento { get; set; }
 public string? TrechoOrigem { get; set; }
 public string[] Tags { get; set; } = [];
 public string StatusRevisao { get; set; } = "pendente";
}
public class InformacaoFinanceira {
 public Guid Id { get; set; } = Guid.NewGuid();
 public Guid ClienteId { get; set; }
 public string Conteudo { get; set; } = "";
 public string? TrechoOrigem { get; set; }
 public string[] Tags { get; set; } = [];
 public string StatusRevisao { get; set; } = "pendente";
}
