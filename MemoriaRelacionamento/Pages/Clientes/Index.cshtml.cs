using MemoriaRelacionamento.Data;
using MemoriaRelacionamento.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace MemoriaRelacionamento.Pages.Clientes;

public class IndexModel(ILogger<IndexModel> logger, AppDbContext? db = null) : PageModel
{
    public bool BancoConfigurado => db is not null;
    public List<Cliente> Clientes { get; private set; } = [];
    public string? Erro { get; private set; }
    public async Task OnGetAsync(CancellationToken ct)
    {
        if (db is null) return;
        try { Clientes = await db.Clientes.AsNoTracking().OrderBy(x => x.Nome).ToListAsync(ct); }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Falha ao listar clientes");
            Erro = "Não foi possível acessar os clientes. Verifique a conexão e o schema do Supabase.";
        }
    }
}
