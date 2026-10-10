namespace MemoriaRelacionamento.Services;

public static class LocalTranscriptCache
{
    public static string? Ler(string audio, bool parcial = false)
    {
        if (string.IsNullOrEmpty(audio)) return null;
        var path = audio + (parcial ? ".parcial.txt" : ".transcricao.txt");
        if (!File.Exists(path)) return null;
        var texto = File.ReadAllText(path);
        return string.IsNullOrWhiteSpace(texto) ? null : texto;
    }

    public static void Salvar(string audio, string texto, bool parcial = false)
    {
        var path = audio + (parcial ? ".parcial.txt" : ".transcricao.txt");
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temporary, texto);
            File.Move(temporary, path, overwrite: true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}
