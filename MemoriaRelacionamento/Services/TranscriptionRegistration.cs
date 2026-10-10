using MemoriaRelacionamento.Services.Fakes;
using Whisper.net;

namespace MemoriaRelacionamento.Services;

public static class TranscriptionRegistration
{
    // A integração do Whisper muda somente este registro e acrescenta a implementação.
    public static IServiceCollection AddTranscription(this IServiceCollection services, IConfiguration configuration)
    {
        if (configuration.GetValue("Fakes:Transcricao", false))
        {
            services.AddScoped<ITranscriptionService, FakeTranscriptionService>();
            return services;
        }
        if (!OperatingSystem.IsWindows())
            throw new InvalidOperationException("A normalização de áudio do Whisper requer Windows.");
        var modelPath = configuration["Whisper:ModelPath"];
        if (string.IsNullOrWhiteSpace(modelPath))
            throw new InvalidOperationException("Configure Whisper:ModelPath com o caminho do modelo local.");
        // Caminhos relativos seguem a raiz da aplicação, também ao iniciar pela solução.
        var contentRoot = configuration["contentRoot"] ?? Directory.GetCurrentDirectory();
        modelPath = Path.GetFullPath(modelPath, contentRoot);
        if (!File.Exists(modelPath))
            throw new InvalidOperationException($"Modelo Whisper não encontrado: {modelPath}. Disponibilize o modelo local ou use Fakes:Transcricao=true.");
        services.AddSingleton(_ => WhisperFactory.FromPath(modelPath));
        services.AddScoped<ITranscriptionService, WhisperTranscriptionService>();
        return services;
    }
}
