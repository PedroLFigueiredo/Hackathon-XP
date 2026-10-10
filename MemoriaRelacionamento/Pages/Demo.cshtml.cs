using MemoriaRelacionamento.Data;
using MemoriaRelacionamento.Models;
using MemoriaRelacionamento.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
namespace MemoriaRelacionamento.Pages;
public class DemoModel(ILogger<DemoModel> logger, AppDbContext? db = null) : PageModel
{
    public bool BancoConfigurado => db is not null;
    public IReadOnlyList<Cliente> Clientes => DemoClientesService.CriarClientes();
    [TempData] public string? Resultado { get; set; }
    public void OnGet() { }
    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        if (db is null) { ModelState.AddModelError("", "Configure a conexão com o banco para criar os clientes."); return Page(); }
        try
        {
            await new DemoClientesService(db).CarregarAsync(ct);
            Resultado = "Os seis clientes fictícios foram salvos no banco. Você já pode abrir seus perfis. Repetir a carga preserva o histórico e não duplica os clientes.";
            return RedirectToPage();
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Falha na carga de demonstração");
            ModelState.AddModelError("", "Não foi possível criar os clientes. Verifique a conexão e aplique o schema enviado, com a vírgula corrigida após email TEXT.");
            return Page();
        }
    }
}
