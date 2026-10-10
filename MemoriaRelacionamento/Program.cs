using MemoriaRelacionamento.Data;
using MemoriaRelacionamento.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
DatabaseConfiguration.Carregar(builder);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddProjectServices(builder.Configuration);
builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = ConversaService.MaxAudioBytes);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

if (!app.Environment.IsDevelopment()) app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapGet("/api/conversas/{id:guid}/status", async (Guid id, IServiceProvider services, CancellationToken ct) =>
{
    var db = services.GetService<AppDbContext>();
    if (db is null) return Results.Problem("Supabase não configurado.", statusCode: 503);
    var conversa = await db.Conversas.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
    return conversa is null ? Results.NotFound() : Results.Ok(new
    {
        status = conversa.Status.ToString(),
        mensagemDeErro = conversa.MensagemDeErro,
        progresso = conversa.Status == MemoriaRelacionamento.Models.StatusConversa.Transcrevendo
            ? services.GetRequiredService<TranscriptionProgressStore>().Obter(id) : null
    });
});
app.Run();
