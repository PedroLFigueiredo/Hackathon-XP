using System.Threading.Channels;

namespace MemoriaRelacionamento.Services;

public class ConversaQueue
{
    private readonly Channel<Guid> queue = Channel.CreateUnbounded<Guid>(new UnboundedChannelOptions { SingleReader = true });
    public void Enfileirar(Guid id) => queue.Writer.TryWrite(id);
    public IAsyncEnumerable<Guid> LerAsync(CancellationToken ct) => queue.Reader.ReadAllAsync(ct);
}
