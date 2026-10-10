using Npgsql;
namespace MemoriaRelacionamento.Services;

public static class DatabaseConfiguration
{
    public static void Carregar(WebApplicationBuilder builder)
    {
        if (!string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("Supabase"))) return;
        var url = builder.Configuration["DATABASE_URL"];
        if (string.IsNullOrWhiteSpace(url) && builder.Environment.IsDevelopment())
        {
            foreach (var directory in new[] { builder.Environment.ContentRootPath, Path.GetDirectoryName(builder.Environment.ContentRootPath) })
            {
                if (directory is null) continue;
                var file = Path.Combine(directory, ".env");
                if (!File.Exists(file)) continue;
                var values = LerArquivo(file, "DATABASE_URL", "ConnectionStrings__Supabase");
                if (values.TryGetValue("ConnectionStrings__Supabase", out var connection) && !string.IsNullOrWhiteSpace(connection))
                {
                    builder.Configuration["ConnectionStrings:Supabase"] = connection;
                    return;
                }
                if (values.TryGetValue("DATABASE_URL", out url) && !string.IsNullOrWhiteSpace(url)) break;
            }
        }
        if (!string.IsNullOrWhiteSpace(url))
            builder.Configuration["ConnectionStrings:Supabase"] = ConverterUrl(url);
    }

    public static string ConverterUrl(string value)
    {
        if (!value.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) && !value.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
            return new NpgsqlConnectionStringBuilder(value).ConnectionString;
        var uri = new Uri(value);
        var credentials = uri.UserInfo.Split(':', 2);
        if (credentials.Length != 2 || string.IsNullOrWhiteSpace(uri.Host) || uri.AbsolutePath.Length <= 1)
            throw new InvalidOperationException("DATABASE_URL deve informar host, banco, usuário e senha PostgreSQL.");
        var connection = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port > 0 ? uri.Port : 5432,
            Database = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/')),
            Username = Uri.UnescapeDataString(credentials[0]),
            Password = Uri.UnescapeDataString(credentials[1]),
            SslMode = SslMode.Require
        };
        foreach (var item in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var pair = item.Split('=', 2);
            if (pair.Length == 2 && pair[0].Equals("sslmode", StringComparison.OrdinalIgnoreCase))
                connection.SslMode = Enum.Parse<SslMode>(Uri.UnescapeDataString(pair[1]).Replace("-", ""), true);
        }
        return connection.ConnectionString;
    }

    internal static Dictionary<string, string> LerArquivo(string file, params string[] chaves)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in File.ReadLines(file))
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("export ", StringComparison.Ordinal)) trimmed = trimmed[7..].TrimStart();
            if (trimmed.Length == 0 || trimmed.StartsWith('#')) continue;
            var pair = trimmed.Split('=', 2);
            if (pair.Length != 2) continue;
            var key = pair[0].Trim();
            if (!chaves.Contains(key, StringComparer.OrdinalIgnoreCase)) continue;
            var value = pair[1].Trim();
            if (value.Length >= 2 && ((value[0] == '\"' && value[^1] == '\"') || (value[0] == '\'' && value[^1] == '\''))) value = value[1..^1];
            values[key] = value;
        }
        return values;
    }
}
