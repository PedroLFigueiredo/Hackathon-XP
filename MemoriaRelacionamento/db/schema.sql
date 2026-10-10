-- Aplicar no SQL Editor do Supabase em um projeto novo. Não apaga dados.
begin;
create table if not exists cliente (
    id uuid primary key default gen_random_uuid(),
    crm_id text not null unique,
    nome text not null,
    telefone text,
    email text,
    criado_em timestamptz not null default now()
);
create table if not exists conversa (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid not null references cliente(id) on delete cascade,
    data date not null,
    origem text not null check (origem in ('Audio', 'Texto')),
    caminho_do_audio text,
    status text not null default 'NaFila' check (status in ('NaFila', 'Transcrevendo', 'Analisando', 'Concluida', 'Erro')),
    transcricao text,
    resumo text[] not null default '{}',
    sentimento text,
    pautas_sugeridas text[] not null default '{}',
    mensagem_de_erro text,
    criado_em timestamptz not null default now()
);
create table if not exists informacao_insight (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid not null references cliente(id) on delete cascade,
    conversa_id uuid not null references conversa(id) on delete cascade,
    categoria text not null,
    conteudo text not null,
    trecho_de_origem text,
    data_referencia date,
    recorrente_anual boolean not null default false
);
create table if not exists acao_todo (
    id uuid primary key default gen_random_uuid(),
    cliente_id uuid not null references cliente(id) on delete cascade,
    conversa_id uuid references conversa(id) on delete cascade,
    descricao text not null,
    data_opcional date,
    concluida boolean not null default false
);
create index if not exists ix_conversa_cliente_data on conversa(cliente_id, data desc);
create index if not exists ix_insight_cliente on informacao_insight(cliente_id);
create index if not exists ix_todo_concluida on acao_todo(concluida);
-- A aplicação usa conexão Postgres no servidor; não expor estas tabelas à API pública.
alter table cliente enable row level security;
alter table conversa enable row level security;
alter table informacao_insight enable row level security;
alter table acao_todo enable row level security;
commit;
