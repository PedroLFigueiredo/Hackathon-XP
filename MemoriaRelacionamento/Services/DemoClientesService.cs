using MemoriaRelacionamento.Data;
using MemoriaRelacionamento.Models;
using Microsoft.EntityFrameworkCore;
namespace MemoriaRelacionamento.Services;
public class DemoClientesService(AppDbContext db)
{
    // Só o cadastro. Perfil financeiro e personalidade vêm das conversas, pela análise.
    public static IReadOnlyList<Cliente> CriarClientes() => [
        new() { Id = Guid.Parse("d0000000-0000-4000-8000-000000000001"), Nome = "Ana Martins", Email = "ana.demo@example.com", Telefone = "11900000001" },
        new() { Id = Guid.Parse("d0000000-0000-4000-8000-000000000002"), Nome = "Bruno Oliveira", Email = "bruno.demo@example.com", Telefone = "11900000002" },
        new() { Id = Guid.Parse("d0000000-0000-4000-8000-000000000003"), Nome = "Carla Santos", Email = "carla.demo@example.com", Telefone = "11900000003" },
        new() { Id = Guid.Parse("d0000000-0000-4000-8000-000000000004"), Nome = "Diego Ferreira", Email = "diego.demo@example.com", Telefone = "11900000004" },
        new() { Id = Guid.Parse("d0000000-0000-4000-8000-000000000005"), Nome = "Elisa Costa", Email = "elisa.demo@example.com", Telefone = "11900000005" },
        new() { Id = Guid.Parse("d0000000-0000-4000-8000-000000000006"), Nome = "Felipe Almeida", Email = "felipe.demo@example.com", Telefone = "11900000006" }
    ];
    public async Task CarregarAsync(CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        // Repetir a carga atualiza só o cadastro: o que a análise gravou no cliente é preservado.
        foreach (var cliente in CriarClientes())
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                insert into clientes (id, nome, email, telefone)
                values ({cliente.Id}, {cliente.Nome}, {cliente.Email}, {cliente.Telefone})
                on conflict (id) do update set nome = excluded.nome, email = excluded.email, telefone = excluded.telefone
                """, ct);
        await transaction.CommitAsync(ct);
    }
}
