using System.Collections.Concurrent;

namespace MemoriaRelacionamento.Services;

// Progresso temporário: a transcrição definitiva só é salva quando estiver completa.
public sealed class TranscriptionProgressStore
{
    private readonly ConcurrentDictionary<Guid, TranscriptionProgress> _progresso = new();

    public void Atualizar(Guid id, TranscriptionProgress progresso) => _progresso[id] = progresso;
    public TranscriptionProgress? Obter(Guid id) => _progresso.GetValueOrDefault(id);
    public void Remover(Guid id) => _progresso.TryRemove(id, out _);
}
