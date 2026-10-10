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
    public List<InformacaoInsight> PerfilPessoal { get; private set; } = [];
    public List<InformacaoEvento> Eventos { get; private set; } = [];
    public List<InformacaoFinanceira> Financeiro { get; private set; } = [];
    public List<AcaoToDo> Tarefas { get; private set; } = [];
    public List<AcaoToDo> Pautas { get; private set; } = [];
    public bool MemoriaVazia => PerfilPessoal.Count + Eventos.Count + Financeiro.Count == 0;
    public List<InsightAssessor> InsightsAssessor { get; private set; } = [];
    public bool InsightsIndisponiveis { get; private set; }
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
            var hoje = DateOnly.FromDateTime(DateTime.Today);
            var pessoais = await db.Insights.AsNoTracking().Where(x => x.ClienteId == id).ToListAsync(ct);
            PerfilPessoal = pessoais.OrderBy(x => Conversas.FindIndex(c => c.Id == x.ConversaId)).ToList();
            var eventos = await db.Eventos.AsNoTracking().Where(x => x.ClienteId == id).ToListAsync(ct);
            // Próximos primeiro; depois os sem data; por último os que já passaram.
            Eventos = eventos
                .Select(x => (Evento: x, Proxima: x.DataEvento is { } data ? Rotulos.ProximaOcorrencia(data, Rotulos.Anual(x.Tags), hoje) : (DateOnly?)null))
                .OrderBy(x => x.Proxima is null ? 1 : x.Proxima < hoje ? 2 : 0)
                .ThenBy(x => x.Proxima >= hoje ? x.Proxima!.Value.DayNumber : -(x.Proxima?.DayNumber ?? 0))
                .Select(x => x.Evento).ToList();
            Financeiro = await db.Financeiro.AsNoTracking().Where(x => x.ClienteId == id).ToListAsync(ct);
            var acoes = await db.ToDos.AsNoTracking().Where(x => x.ClienteId == id).ToListAsync(ct);
            // Pendentes primeiro, com o prazo mais próximo no topo.
            var ordenadas = acoes.OrderBy(x => x.Concluida).ThenBy(x => x.DataOpcional ?? DateOnly.MaxValue).ToList();
            Pautas = ordenadas.Where(x => x.Tags?.Contains(TagsReservadas.Pauta) == true).ToList();
            Tarefas = ordenadas.Except(Pautas).ToList();
            try
            {
                var insights = await db.InsightsAssessor.AsNoTracking().Where(x => x.ClienteId == id).ToListAsync(ct);
                // Mais recentes primeiro, na ordem das conversas já carregadas.
                InsightsAssessor = insights.OrderBy(x => Conversas.FindIndex(c => c.Id == x.ConversaId)).ToList();
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                // A tabela é posterior ao schema original; sem ela o resto do perfil continua abrindo.
                logger.LogError(exception, "Falha ao ler insights do assessor de {ClienteId}", id);
                InsightsIndisponiveis = true;
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Falha ao abrir perfil {ClienteId}", id);
            ModelState.AddModelError("", "Não foi possível abrir o perfil. Verifique a conexão e o schema do Supabase.");
        }
        return Page();
    }

    public string? DataDaConversa(Guid? conversaId) => Conversas.FirstOrDefault(x => x.Id == conversaId)?.Data.ToString("dd/MM/yyyy");

    public async Task<IActionResult> OnPostAlternarAcaoAsync(Guid id, Guid acaoId, CancellationToken ct)
    {
        if (db is null) return StatusCode(503);
        try
        {
            var acao = await db.ToDos.SingleOrDefaultAsync(x => x.Id == acaoId && x.ClienteId == id, ct);
            if (acao is not null)
            {
                acao.Concluida = !acao.Concluida;
                await db.SaveChangesAsync(ct);
            }
            return RedirectToPage(pageName: null, pageHandler: null, routeValues: new { id }, fragment: "acoes");
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Falha ao atualizar a ação {AcaoId}", acaoId);
            ModelState.AddModelError("", "Não foi possível atualizar a ação. Tente novamente.");
        }
        return await OnGetAsync(id, ct);
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
