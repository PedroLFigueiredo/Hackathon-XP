using MemoriaRelacionamento.Services;
using Npgsql;
namespace MemoriaRelacionamento.Tests;
public class DatabaseConfigurationTests
{
    [Fact]
    public void Url_decodifica_credenciais_e_preserva_porta()
    {
        var c = new NpgsqlConnectionStringBuilder(DatabaseConfiguration.ConverterUrl("postgresql://postgres.demo:senha%40%3A%2F@localhost:6543/postgres?sslmode=require"));
        Assert.Equal("postgres.demo", c.Username);
        Assert.Equal("senha@:/", c.Password);
        Assert.Equal(6543, c.Port);
        Assert.Equal("postgres", c.Database);
        Assert.Equal(SslMode.Require, c.SslMode);
    }
    [Fact]
    public void Url_sem_porta_usa_5432_e_ssl()
    {
        var c = new NpgsqlConnectionStringBuilder(DatabaseConfiguration.ConverterUrl("postgres://demo:senha@localhost/postgres"));
        Assert.Equal(5432, c.Port);
        Assert.Equal(SslMode.Require, c.SslMode);
    }
    [Fact]
    public void Aceita_connection_string_Npgsql()
    {
        var c = new NpgsqlConnectionStringBuilder(DatabaseConfiguration.ConverterUrl("Host=localhost;Database=postgres;Username=demo;Password=senha"));
        Assert.Equal("localhost", c.Host);
    }
}
