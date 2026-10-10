using MemoriaRelacionamento.Data;
using MemoriaRelacionamento.Models;
using MemoriaRelacionamento.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace MemoriaRelacionamento.Pages.Clientes;

[RequestSizeLimit(ConversaService.MaxAudioBytes)]
[RequestFormLimits(MultipartBodyLengthLimit = ConversaService.MaxAudioBytes)]
public class DetalheModel(ILogger<DetalheModel> logger, AppDbContext? db = null, ConversaService? service = null) : PageModel
{
    public bool BancoConfigurado => db is not null;
    public Cliente? Cliente { get; private set; }
    public List<Conversa> Conversas { get; private set; } = [];
    public HashSet<Guid> TranscricoesParciais { get; } = [];
    public List<InformacaoInsight> Insights { get; private set; } = [];
    public List<AcaoToDo> Tarefas { get; private set; } = [];
    [BindProperty] public DateOnly Data { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    [BindProperty] public string? Texto { get; set; }
    [BindProperty] public IFormFile? Audio { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken ct)
    {
        if (db is null) return Page();
        try
        {
            Cliente = await db.Clientes.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
            if (Cliente is null) return NotFound();
            Conversas = await db.Conversas.AsNoTracking().Where(x => x.ClienteId == id).OrderByDescending(x => x.DataRegistro).ToListAsync(ct);
            foreach (var conversa in Conversas.Where(x => string.IsNullOrWhiteSpace(x.Transcricao)))
            {
                try
                {
                    conversa.Transcricao = LocalTranscriptCache.Ler(conversa.CaminhoDoAudio ?? "");
                    if (conversa.Transcricao is null)
                    {
                        conversa.Transcricao = LocalTranscriptCache.Ler(conversa.CaminhoDoAudio ?? "", parcial: true);
                        if (conversa.Transcricao is not null) TranscricoesParciais.Add(conversa.Id);
                    }
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                { logger.LogWarning(ex, "Não foi possível ler a transcrição local de {ConversaId}", conversa.Id); }
            }
            Insights = await db.Insights.AsNoTracking().Where(x => x.ClienteId == id).ToListAsync(ct);
            var eventos = await db.Eventos.AsNoTracking().Where(x => x.ClienteId == id).ToListAsync(ct);
            Insights.AddRange(eventos.Select(x => new InformacaoInsight { Id = x.Id, ClienteId = id, Categoria = CategoriaInsight.Evento, Conteudo = x.Conteudo, TrechoDeOrigem = x.TrechoOrigem, DataReferencia = x.DataEvento }));
            var financeiro = await db.Financeiro.AsNoTracking().Where(x => x.ClienteId == id).ToListAsync(ct);
            Insights.AddRange(financeiro.Select(x => new InformacaoInsight { Id = x.Id, ClienteId = id, Categoria = CategoriaInsight.Objetivo, Conteudo = x.Conteudo, TrechoDeOrigem = x.TrechoOrigem }));
            Tarefas = await db.ToDos.AsNoTracking().Where(x => x.ClienteId == id).OrderBy(x => x.StatusConclusao).ToListAsync(ct);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Falha ao abrir perfil {ClienteId}", id);
            ModelState.AddModelError("", "Não foi possível abrir o perfil. Verifique a conexão e o schema do Supabase.");
        }
        return Page();
    }

    public async Task<IActionResult> OnPostTextoAsync(Guid id, CancellationToken ct) => await EnviarAsync(id, false, ct);
    public async Task<IActionResult> OnPostAudioAsync(Guid id, CancellationToken ct) => await EnviarAsync(id, true, ct);

    public async Task<IActionResult> OnPostReprocessarAsync(Guid id, Guid conversaId, CancellationToken ct)
    {
        if (service is null) return StatusCode(503);
        try
        {
            await service.ReprocessarAsync(id, conversaId, ct);
            return RedirectToPage(new { id });
        }
        catch (ArgumentException ex) { ModelState.AddModelError("", ex.Message); }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Falha ao reprocessar conversa {ConversaId}", conversaId);
            ModelState.AddModelError("", "Não foi possível colocar a conversa na fila. Tente novamente.");
        }
        return await OnGetAsync(id, ct);
    }

    private async Task<IActionResult> EnviarAsync(Guid id, bool audio, CancellationToken ct)
    {
        if (service is null) return StatusCode(503);
        if (!ModelState.IsValid) return await OnGetAsync(id, ct);
        try
        {
            if (audio)
            {
                if (Audio is null || Audio.Length == 0) throw new ArgumentException("Selecione um áudio.");
                await using var stream = Audio.OpenReadStream();
                await service.CriarDeAudioAsync(id, Data, stream, Audio.FileName, ct);
            }
            else await service.CriarDeTextoAsync(id, Data, Texto ?? "", ct);
            return RedirectToPage(new { id });
        }
        catch (ArgumentException exception) { ModelState.AddModelError("", exception.Message); }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Falha ao enviar conversa");
            ModelState.AddModelError("", "Não foi possível salvar a conversa. Verifique a conexão com o Supabase.");
        }
        return await OnGetAsync(id, ct);
    }
}
