using MemoriaRelacionamento.Data;
using MemoriaRelacionamento.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace MemoriaRelacionamento.Pages;

/// <summary>Insights para o assessor, um cliente por vez: o assessor escolhe o cliente e vê só os sinais dele.</summary>
public class InsightsModel(ILogger<InsightsModel> logger, AppDbContext? db = null) : PageModel
{
    public record ClienteDaLista(Cliente Cliente, int Insights);

    public bool BancoConfigurado => db is not null;
    public string? Erro { get; private set; }
    public List<ClienteDaLista> Clientes { get; private set; } = [];
    public Cliente? Selecionado { get; private set; }
    public List<InsightCartao> Insights { get; private set; } = [];
    public int Conversas { get; private set; }

    /// <summary>Cliente escolhido, em /Insights?cliente={id}.</summary>
    [BindProperty(SupportsGet = true, Name = "cliente")] public Guid? ClienteId { get; set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        if (db is null) return Page();
        try
        {
            var clientes = await db.Clientes.AsNoTracking().OrderBy(x => x.Nome).ToListAsync(ct);
            var totais = await db.InsightsAssessor.AsNoTracking().GroupBy(x => x.ClienteId)
                .Select(x => new { ClienteId = x.Key, Total = x.Count() }).ToDictionaryAsync(x => x.ClienteId, x => x.Total, ct);
            Clientes = clientes.Select(x => new ClienteDaLista(x, totais.GetValueOrDefault(x.Id))).ToList();
            if (ClienteId is null) return Page();

            Selecionado = clientes.SingleOrDefault(x => x.Id == ClienteId);
            if (Selecionado is null) return NotFound();
            var insights = await db.InsightsAssessor.AsNoTracking().Where(x => x.ClienteId == ClienteId).ToListAsync(ct);
            var conversas = await db.Conversas.AsNoTracking().Where(x => x.ClienteId == ClienteId).ToDictionaryAsync(x => x.Id, ct);
            // Sinais das conversas mais recentes primeiro.
            Insights = insights
                .OrderByDescending(x => conversas.GetValueOrDefault(x.ConversaId)?.DataRegistro)
                .Select(x => new InsightCartao(x, conversas.GetValueOrDefault(x.ConversaId)?.Data.ToString("dd/MM/yyyy"))).ToList();
            Conversas = insights.Select(x => x.ConversaId).Distinct().Count();
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Falha ao carregar insights");
            Erro = "Não foi possível carregar os insights. Verifique a conexão e se a tabela insights_assessor existe no banco (MemoriaRelacionamento/db/insights_assessor.sql).";
        }
        return Page();
    }
}
