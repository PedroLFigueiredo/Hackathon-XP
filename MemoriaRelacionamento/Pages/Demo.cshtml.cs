using MemoriaRelacionamento.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic.FileIO;

namespace MemoriaRelacionamento.Pages;

public class DemoModel(IWebHostEnvironment environment, ILogger<DemoModel> logger, AppDbContext? db = null) : PageModel
{
    public bool BancoConfigurado => db is not null;
    public List<string[]> Clientes { get; private set; } = [];
    [TempData] public string? Resultado { get; set; }

    public IActionResult OnGet()
    {
        if (!environment.IsDevelopment()) return NotFound();
        CarregarPreview();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        if (!environment.IsDevelopment()) return NotFound();
        CarregarPreview();
        if (db is null) return Page();
        try
        {
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            foreach (var cliente in Clientes)
            {
                await db.Database.ExecuteSqlInterpolatedAsync($"""
                    insert into cliente (id, crm_id, nome, email, telefone, criado_em)
                    values ({Guid.NewGuid()}, {cliente[0]}, {cliente[1]}, {cliente[2]}, {cliente[3]}, {DateTime.UtcNow})
                    on conflict (crm_id) do update set nome = excluded.nome, email = excluded.email, telefone = excluded.telefone
                    """, ct);
            }
            await transaction.CommitAsync(ct);
            Resultado = "Os seis clientes de demonstração foram carregados. Cadastros existentes foram atualizados sem duplicação.";
            return RedirectToPage();
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Falha na carga de demonstração");
            ModelState.AddModelError("", "Não foi possível carregar os clientes. Verifique a conexão e aplique o schema do Supabase.");
            return Page();
        }
    }

    private void CarregarPreview()
    {
        using var parser = new TextFieldParser(Path.Combine(environment.WebRootPath, "exemplos", "clientes.csv"));
        parser.SetDelimiters(",");
        parser.HasFieldsEnclosedInQuotes = true;
        parser.ReadFields();
        while (!parser.EndOfData)
        {
            var fields = parser.ReadFields();
            if (fields is { Length: 4 }) Clientes.Add(fields);
        }
    }
}
