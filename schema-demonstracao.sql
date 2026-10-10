-- Tabela: Clientes
CREATE TABLE clientes (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    nome TEXT NOT NULL,
    telefone TEXT,
    email TEXT,
    perfil_financeiro TEXT, -- Ex: Conservador, Moderado, Arrojado
    personalidade TEXT      -- Ex: Analítico, Comunicativo, Impetuoso, Reservado
);

-- Tabela: Conversas
CREATE TABLE conversas (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    cliente_id UUID NOT NULL REFERENCES clientes(id) ON DELETE CASCADE,
    data TIMESTAMP WITH TIME ZONE DEFAULT now(),
    caminho_audio TEXT NOT NULL,
    tom TEXT,
    status TEXT NOT NULL, -- Ex: aguardando, transcrevendo, analisando, concluído, erro
    transcricao TEXT,
    resumo TEXT,
    mensagem_erro TEXT
);

-- Tabela: Informação Pessoal
CREATE TABLE perfil_pessoal (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    cliente_id UUID NOT NULL REFERENCES clientes(id) ON DELETE CASCADE,
    conversa_id UUID NOT NULL REFERENCES conversas(id) ON DELETE CASCADE,
    conteudo TEXT NOT NULL,
    trecho_origem TEXT,
    tags TEXT[], -- Array de tags / palavras-chave
    status_revisao TEXT NOT NULL DEFAULT 'pendente' -- Ex: pendente, aprovado, descartado
);

-- Tabela: Informação de Eventos
CREATE TABLE informacao_eventos (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    cliente_id UUID NOT NULL REFERENCES clientes(id) ON DELETE CASCADE,
    conteudo TEXT NOT NULL,
    data_evento DATE, -- Útil caso o LLM extraia a data específica
    trecho_origem TEXT,
    tags TEXT[], -- Array de tags / palavras-chave
    status_revisao TEXT NOT NULL DEFAULT 'pendente'
);

-- Tabela: Informação Financeira
CREATE TABLE informacao_financeiro (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    cliente_id UUID NOT NULL REFERENCES clientes(id) ON DELETE CASCADE,
    conteudo TEXT NOT NULL,
    trecho_origem TEXT,
    tags TEXT[], -- Array de tags / palavras-chave
    status_revisao TEXT NOT NULL DEFAULT 'pendente'
);

-- Tabela: Ações (Próximas ações e compromissos do consultor)
CREATE TABLE acoes (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    cliente_id UUID NOT NULL REFERENCES clientes(id) ON DELETE CASCADE,
    conversa_id UUID NOT NULL REFERENCES conversas(id) ON DELETE CASCADE,
    descricao TEXT NOT NULL,
    data_opcional DATE,
    tags TEXT[], -- Array de tags / palavras-chave
    status_conclusao TEXT NOT NULL DEFAULT 'pendente' -- Ex: pendente, concluido
);

-- Tabela: Insights do Assessor
CREATE TABLE IF NOT EXISTS insights_assessor (
      id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
      cliente_id UUID NOT NULL REFERENCES clientes(id) ON DELETE CASCADE,
      conversa_id UUID NOT NULL REFERENCES conversas(id) ON DELETE CASCADE,
      tipo TEXT NOT NULL,
      conteudo TEXT NOT NULL,
      sugestao TEXT,
      trecho_origem TEXT
  );

  -- Ativação de segurança (RLS) para os insights do assessor
  ALTER TABLE insights_assessor ENABLE ROW LEVEL SECURITY;