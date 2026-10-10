namespace MemoriaRelacionamento.Models;

public class Cliente
{
    public Guid Id { get; set; } = Guid.NewGuid();
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string CrmId { get; set; } = "";
    public string Nome { get; set; } = "";
    public string? Telefone { get; set; }
    public string? Email { get; set; }
    public string? PerfilFinanceiro { get; set; }
    public string? Personalidade { get; set; }
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}
