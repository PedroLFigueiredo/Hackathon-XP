using MemoriaRelacionamento.Data;
using MemoriaRelacionamento.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace MemoriaRelacionamento.Pages.Clientes;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    public IndexModel(AppDbContext db) => _db = db;

    public List<Cliente> Clientes { get; private set; } = new();

    [BindProperty] public string? Nome { get; set; }
    [BindProperty] public string? Telefone { get; set; }
    [BindProperty] public string? Email { get; set; }

    public async Task OnGetAsync() => await CarregarAsync();

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(Nome))
        {
            ModelState.AddModelError(nameof(Nome), "O nome é obrigatório.");
            await CarregarAsync();
            return Page();
        }
        _db.Clientes.Add(new Cliente { Id = Guid.NewGuid(), Nome = Nome.Trim(), Telefone = Telefone, Email = Email });
        await _db.SaveChangesAsync();
        return RedirectToPage();
    }

    private async Task CarregarAsync() =>
        Clientes = await _db.Clientes.AsNoTracking().OrderBy(c => c.Nome).ToListAsync();
}
