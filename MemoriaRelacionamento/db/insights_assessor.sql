-- Tabela: Insights para o assessor (sinais percebidos pela IA em cada conversa).
-- Rodar uma vez no SQL Editor do Supabase. Não altera nem apaga as tabelas existentes.
CREATE TABLE IF NOT EXISTS insights_assessor (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    cliente_id UUID NOT NULL REFERENCES clientes(id) ON DELETE CASCADE,
    conversa_id UUID NOT NULL REFERENCES conversas(id) ON DELETE CASCADE,
    tipo TEXT NOT NULL, -- apetite_risco, capacidade_financeira, oportunidade, alerta
    conteudo TEXT NOT NULL, -- O que foi percebido e por que importa
    sugestao TEXT, -- Como o assessor pode conduzir a conversa
    trecho_origem TEXT -- Fala do cliente que sustenta o sinal
);

-- A aplicação acessa pela conexão Postgres do servidor; sem políticas, a API pública do Supabase não lê a tabela.
ALTER TABLE insights_assessor ENABLE ROW LEVEL SECURITY;
