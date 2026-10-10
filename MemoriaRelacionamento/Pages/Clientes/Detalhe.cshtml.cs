using MemoriaRelacionamento.Data;
using MemoriaRelacionamento.Models;
using MemoriaRelacionamento.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace MemoriaRelacionamento.Pages.Clientes;

[RequestSizeLimit(200_000_000)]
[RequestFormLimits(MultipartBodyLengthLimit = 200_000_000)]
public class DetalheModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly ConversaService _conversas;

    public DetalheModel(AppDbContext db, ConversaService conversas)
    {
        _db = db;
        _conversas = conversas;
    }

    public Cliente Cliente { get; private set; } = null!;
    public List<Conversa> Conversas { get; private set; } = new();

    [BindProperty] public DateOnly Data { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    [BindProperty] public string? Texto { get; set; }
    [BindProperty] public IFormFile? Audio { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        if (!await CarregarAsync(id)) return NotFound();
        return Page();
    }

    public async Task<IActionResult> OnPostTextoAsync(Guid id, CancellationToken ct)
    {
        if (!await CarregarAsync(id)) return NotFound();
        try
        {
            await _conversas.CriarDeTextoAsync(id, Data, Texto ?? "", ct);
            return RedirectToPage(new { id });
        }
        catch (ArgumentException ex)
        {
            ModelState.AddModelError(nameof(Texto), ex.Message);
            return Page();
        }
    }

    public async Task<IActionResult> OnPostAudioAsync(Guid id, CancellationToken ct)
    {
        if (!await CarregarAsync(id)) return NotFound();
        if (Audio is null || Audio.Length == 0)
        {
            ModelState.AddModelError(nameof(Audio), "Selecione um arquivo.");
            return Page();
        }
        try
        {
            await using var stream = Audio.OpenReadStream();
            await _conversas.CriarDeAudioAsync(id, Data, stream, Audio.FileName, ct);
            return RedirectToPage(new { id });
        }
        catch (ArgumentException ex)
        {
            ModelState.AddModelError(nameof(Audio), ex.Message);
            return Page();
        }
    }

    private async Task<bool> CarregarAsync(Guid id)
    {
        var cliente = await _db.Clientes.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
        if (cliente is null) return false;
        Cliente = cliente;
        Conversas = await _db.Conversas.AsNoTracking()
            .Where(c => c.ClienteId == id)
            .OrderByDescending(c => c.CriadoEm)
            .ToListAsync();
        return true;
    }
}
