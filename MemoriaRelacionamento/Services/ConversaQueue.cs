using System.Threading.Channels;

namespace MemoriaRelacionamento.Services;

public class ConversaQueue
{
    private readonly Channel<Guid> _canal = Channel.CreateUnbounded<Guid>(
        new UnboundedChannelOptions { SingleReader = true });

    public void Enfileirar(Guid conversaId) => _canal.Writer.TryWrite(conversaId);

    public ValueTask<Guid> LerAsync(CancellationToken ct) => _canal.Reader.ReadAsync(ct);
}
