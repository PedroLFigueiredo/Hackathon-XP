using MemoriaRelacionamento.Models;

namespace MemoriaRelacionamento.Pages;

/// <summary>Dados do cartão de insight (_InsightCartao), usado no perfil do cliente e na tela de insights.</summary>
public record InsightCartao(InsightAssessor Insight, string? DataConversa);
