using MemoriaRelacionamento.Data;
using MemoriaRelacionamento.Models;
using Microsoft.EntityFrameworkCore;
namespace MemoriaRelacionamento.Services;
public class DemoClientesService(AppDbContext db)
{
    public static IReadOnlyList<Cliente> CriarClientes() => [
        new() { Id = Guid.Parse("d0000000-0000-4000-8000-000000000001"), Nome = "Ana Martins", Email = "ana.demo@example.com", Telefone = "11900000001", PerfilFinanceiro = "Conservador", Personalidade = "Analítico" },
        new() { Id = Guid.Parse("d0000000-0000-4000-8000-000000000002"), Nome = "Bruno Oliveira", Email = "bruno.demo@example.com", Telefone = "11900000002", PerfilFinanceiro = "Moderado", Personalidade = "Comunicativo" },
        new() { Id = Guid.Parse("d0000000-0000-4000-8000-000000000003"), Nome = "Carla Santos", Email = "carla.demo@example.com", Telefone = "11900000003", PerfilFinanceiro = "Arrojado", Personalidade = "Impetuoso" },
        new() { Id = Guid.Parse("d0000000-0000-4000-8000-000000000004"), Nome = "Diego Ferreira", Email = "diego.demo@example.com", Telefone = "11900000004", PerfilFinanceiro = "Conservador", Personalidade = "Reservado" },
        new() { Id = Guid.Parse("d0000000-0000-4000-8000-000000000005"), Nome = "Elisa Costa", Email = "elisa.demo@example.com", Telefone = "11900000005", PerfilFinanceiro = "Moderado", Personalidade = "Analítico" },
        new() { Id = Guid.Parse("d0000000-0000-4000-8000-000000000006"), Nome = "Felipe Almeida", Email = "felipe.demo@example.com", Telefone = "11900000006", PerfilFinanceiro = "Arrojado", Personalidade = "Comunicativo" }
    ];
    public async Task CarregarAsync(CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        foreach (var cliente in CriarClientes())
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                insert into clientes (id, nome, email, telefone, perfil_financeiro, personalidade)
                values ({cliente.Id}, {cliente.Nome}, {cliente.Email}, {cliente.Telefone}, {cliente.PerfilFinanceiro}, {cliente.Personalidade})
                on conflict (id) do update set nome = excluded.nome, email = excluded.email,
                    telefone = excluded.telefone, perfil_financeiro = excluded.perfil_financeiro, personalidade = excluded.personalidade
                """, ct);
        await transaction.CommitAsync(ct);
    }
}
