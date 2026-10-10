using MemoriaRelacionamento.Data;
using MemoriaRelacionamento.Services.Fakes;
using Microsoft.EntityFrameworkCore;

namespace MemoriaRelacionamento.Services;

public static class ServiceRegistration
{
    public static IServiceCollection AddProjectServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connection = configuration.GetConnectionString("Supabase");
        if (!string.IsNullOrWhiteSpace(connection))
        {
            services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connection));
            services.AddScoped<ConversaService>();
            services.AddScoped<ConversaProcessor>();
            services.AddSingleton<ConversaQueue>();
            services.AddHostedService<ConversaWorker>();
        }
        services.AddTranscription(configuration);
        if (!configuration.GetValue("Fakes:Analise", true))
            throw new InvalidOperationException("A análise real ainda não foi integrada. Use Fakes:Analise=true.");
        services.AddScoped<IConversationAnalysisService, FakeConversationAnalysisService>();
        return services;
    }
}
