using MemoriaRelacionamento.Services;
using MemoriaRelacionamento.Services.Fakes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MemoriaRelacionamento.Tests;

public class TranscriptionRegistrationTests
{
    [Fact]
    public void Sem_modelo_nao_recorre_a_transcricao_ficticia()
    {
        var configuration = new ConfigurationBuilder().Build();
        Assert.Throws<InvalidOperationException>(() => new ServiceCollection().AddTranscription(configuration));
    }

    [Fact]
    public void Simulacao_precisa_ser_ativada_explicitamente()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["Fakes:Transcricao"] = "true" }).Build();
        using var provider = new ServiceCollection().AddTranscription(configuration).BuildServiceProvider();
        using var scope = provider.CreateScope();
        Assert.IsType<FakeTranscriptionService>(scope.ServiceProvider.GetRequiredService<ITranscriptionService>());
    }
}
