using MemoriaRelacionamento.Data;
using Microsoft.EntityFrameworkCore;

namespace MemoriaRelacionamento.Services;

public static class ServiceRegistration
{
    public static IServiceCollection AddProjectServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<TranscriptionProgressStore>();
        var connection = configuration.GetConnectionString("Supabase");
        if (!string.IsNullOrWhiteSpace(connection))
        {
            services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connection,
                postgres => postgres.EnableRetryOnFailure(3)));
            services.AddScoped<ConversaService>();
            services.AddScoped<ConversaProcessor>();
            services.AddSingleton<ConversaQueue>();
            services.AddHostedService<ConversaWorker>();
        }
        services.AddTranscription(configuration);
        services.AddAnalysis(configuration);
        return services;
    }
}
