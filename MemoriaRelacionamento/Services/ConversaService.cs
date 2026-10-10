using MemoriaRelacionamento.Data;
using MemoriaRelacionamento.Models;

namespace MemoriaRelacionamento.Services;

public class ConversaService
{
    public const int TamanhoMinimoTexto = 50;
    public static readonly string[] ExtensoesAceitas = { ".wav", ".mp3", ".m4a" };

    private readonly AppDbContext _db;
    private readonly ConversaQueue _fila;
    private readonly string _pastaUploads;

    public ConversaService(AppDbContext db, ConversaQueue fila, IConfiguration config, IHostEnvironment env)
    {
        _db = db;
        _fila = fila;
        var pasta = config["Uploads:Path"] ?? "uploads";
        _pastaUploads = Path.IsPathRooted(pasta) ? pasta : Path.Combine(env.ContentRootPath, pasta);
    }

    public async Task<Guid> CriarDeTextoAsync(Guid clienteId, DateOnly data, string texto, CancellationToken ct)
    {
        texto = (texto ?? "").Trim();
        if (texto.Length < TamanhoMinimoTexto)
            throw new ArgumentException($"O texto precisa ter pelo menos {TamanhoMinimoTexto} caracteres.");

        var conversa = new Conversa
        {
            Id = Guid.NewGuid(),
            ClienteId = clienteId,
            Data = data,
            Origem = OrigemConversa.Texto,
            Transcricao = texto,
            Status = StatusConversa.NaFila,
        };
        _db.Conversas.Add(conversa);
        await _db.SaveChangesAsync(ct);
        _fila.Enfileirar(conversa.Id);
        return conversa.Id;
    }

    public async Task<Guid> CriarDeAudioAsync(Guid clienteId, DateOnly data, Stream arquivo, string nomeArquivo, CancellationToken ct)
    {
        var ext = Path.GetExtension(nomeArquivo ?? "").ToLowerInvariant();
        if (!ExtensoesAceitas.Contains(ext))
            throw new ArgumentException($"Formato não suportado. Envie {string.Join(", ", ExtensoesAceitas)}.");

        var id = Guid.NewGuid();
        Directory.CreateDirectory(_pastaUploads);
        var caminho = Path.Combine(_pastaUploads, id + ext);
        await using (var destino = File.Create(caminho))
            await arquivo.CopyToAsync(destino, ct);

        try
        {
            _db.Conversas.Add(new Conversa
            {
                Id = id,
                ClienteId = clienteId,
                Data = data,
                Origem = OrigemConversa.Audio,
                CaminhoDoAudio = caminho,
                Status = StatusConversa.NaFila,
            });
            await _db.SaveChangesAsync(ct);
        }
        catch
        {
            File.Delete(caminho);
            throw;
        }

        _fila.Enfileirar(id);
        return id;
    }
}
