using System.Net.Http.Headers;
using MemoriaRelacionamento.Services.Fakes;

namespace MemoriaRelacionamento.Services;

public static class AnalysisRegistration
{
    public static string? ChaveOpenAi(IConfiguration configuration)
    {
        var chave = configuration["OpenAI:ApiKey"];
        if (string.IsNullOrWhiteSpace(chave)) chave = configuration["OPENAI_API_KEY"];
        return string.IsNullOrWhiteSpace(chave) ? null : chave.Trim();
    }

    public static bool AnaliseRealConfigurada(IConfiguration configuration) =>
        !configuration.GetValue("Fakes:Analise", false) && ChaveOpenAi(configuration) is not null;

    /// <summary>Em Development, lê OPENAI_API_KEY do .env da aplicação ou da raiz do repositório.</summary>
    public static void CarregarChaveLocal(WebApplicationBuilder builder)
    {
        if (ChaveOpenAi(builder.Configuration) is not null || !builder.Environment.IsDevelopment()) return;
        foreach (var directory in new[] { builder.Environment.ContentRootPath, Path.GetDirectoryName(builder.Environment.ContentRootPath) })
        {
            if (directory is null) continue;
            var file = Path.Combine(directory, ".env");
            if (!File.Exists(file)) continue;
            var values = DatabaseConfiguration.LerArquivo(file, "OPENAI_API_KEY", "OpenAI__ApiKey");
            var chave = values.GetValueOrDefault("OpenAI__ApiKey") is { Length: > 0 } especifica ? especifica : values.GetValueOrDefault("OPENAI_API_KEY");
            if (string.IsNullOrWhiteSpace(chave)) continue;
            builder.Configuration["OpenAI:ApiKey"] = chave;
            return;
        }
    }

    // Sem chave, a aplicação continua subindo e preserva só a transcrição.
    public static IServiceCollection AddAnalysis(this IServiceCollection services, IConfiguration configuration)
    {
        if (configuration.GetValue("Fakes:Analise", false))
        {
            services.AddScoped<IConversationAnalysisService, FakeConversationAnalysisService>();
            return services;
        }
        var chave = ChaveOpenAi(configuration);
        if (chave is null)
        {
            services.AddScoped<IConversationAnalysisService, TranscriptOnlyAnalysisService>();
            return services;
        }
        services.AddHttpClient<IConversationAnalysisService, OpenAiConversationAnalysisService>(client =>
        {
            client.BaseAddress = new Uri("https://api.openai.com/");
            client.Timeout = TimeSpan.FromSeconds(120);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", chave);
        });
        return services;
    }
}
