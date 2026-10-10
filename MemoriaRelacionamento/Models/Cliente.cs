namespace MemoriaRelacionamento.Models;

public class Cliente
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string CrmId { get; set; } = "";
    public string Nome { get; set; } = "";
    public string? Telefone { get; set; }
    public string? Email { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}
