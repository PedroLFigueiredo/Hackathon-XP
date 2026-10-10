using MemoriaRelacionamento.Data;
using MemoriaRelacionamento.Models;
using Microsoft.EntityFrameworkCore;

namespace MemoriaRelacionamento.Services;

public class ConversaService(AppDbContext db, ConversaQueue queue, IWebHostEnvironment environment)
{
    public const long MaxAudioBytes = 200_000_000;

    public async Task ReprocessarAsync(Guid clienteId, Guid conversaId, CancellationToken ct)
    {
        var conversa = await db.Conversas.SingleOrDefaultAsync(x => x.Id == conversaId && x.ClienteId == clienteId, ct);
        if (conversa is null) throw new ArgumentException("Conversa não encontrada.");
        if (conversa.Status != StatusConversa.Erro) throw new ArgumentException("Esta conversa não está com erro.");
        conversa.Status = StatusConversa.NaFila;
        conversa.MensagemDeErro = null;
        await db.SaveChangesAsync(ct);
        queue.Enfileirar(conversa.Id);
    }

    public async Task<Guid> CriarDeTextoAsync(Guid clienteId, DateOnly data, string texto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(texto) || texto.Trim().Length < 50)
            throw new ArgumentException("Informe uma transcrição com pelo menos 50 caracteres.");
        return await SalvarAsync(new Conversa { ClienteId = clienteId, Data = data, Origem = OrigemConversa.Texto, Transcricao = texto.Trim() }, ct);
    }

    public async Task<Guid> CriarDeAudioAsync(Guid clienteId, DateOnly data, Stream arquivo, string nomeArquivo, CancellationToken ct)
    {
        var extension = Path.GetExtension(nomeArquivo).ToLowerInvariant();
        if (extension is not (".wav" or ".mp3" or ".m4a")) throw new ArgumentException("Envie um arquivo WAV, MP3 ou M4A.");
        var conversa = new Conversa { ClienteId = clienteId, Data = data, Origem = OrigemConversa.Audio };
        var directory = Path.Combine(environment.ContentRootPath, "uploads");
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, $"{conversa.Id}{extension}");
        try
        {
            await using (var destination = File.Create(path))
            {
                var buffer = new byte[81920];
                long total = 0;
                int count;
                while ((count = await arquivo.ReadAsync(buffer, ct)) > 0)
                {
                    total += count;
                    if (total > MaxAudioBytes) throw new ArgumentException("O áudio deve ter no máximo 200 MB.");
                    await destination.WriteAsync(buffer.AsMemory(0, count), ct);
                }
                if (total == 0) throw new ArgumentException("O arquivo de áudio está vazio.");
            }
            conversa.CaminhoDoAudio = path;
            return await SalvarAsync(conversa, ct);
        }
        catch { File.Delete(path); throw; }
    }

    private async Task<Guid> SalvarAsync(Conversa conversa, CancellationToken ct)
    {
        if (!await db.Clientes.AnyAsync(x => x.Id == conversa.ClienteId, ct)) throw new ArgumentException("Cliente não encontrado.");
        db.Conversas.Add(conversa);
        await db.SaveChangesAsync(ct);
        queue.Enfileirar(conversa.Id);
        return conversa.Id;
    }
}
