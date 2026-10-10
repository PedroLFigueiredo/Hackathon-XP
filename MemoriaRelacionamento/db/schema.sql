-- Recria o schema do zero. Rodado por `dotnet run -- --init-db` (apaga os dados existentes).
drop table if exists acao_todo cascade;
drop table if exists informacao_insight cascade;
drop table if exists conversa cascade;
drop table if exists cliente cascade;

create table cliente (
  id uuid primary key default gen_random_uuid(),
  nome text not null,
  telefone text,
  email text,
  criado_em timestamptz not null default now()
);

create table conversa (
  id uuid primary key default gen_random_uuid(),
  cliente_id uuid not null references cliente(id) on delete cascade,
  data date not null,                 -- data da reunião
  origem text not null,               -- Audio | Texto
  caminho_do_audio text,              -- null quando origem = Texto
  status text not null default 'NaFila',
  transcricao text,
  resumo text[],
  sentimento text,
  pautas_sugeridas text[],
  mensagem_de_erro text,
  criado_em timestamptz not null default now()
);

create table informacao_insight (
  id uuid primary key default gen_random_uuid(),
  cliente_id uuid not null references cliente(id) on delete cascade,
  conversa_id uuid not null references conversa(id) on delete cascade,
  categoria text not null,
  conteudo text not null,
  trecho_de_origem text,
  data_referencia date,
  recorrente_anual boolean not null default false,
  status_de_revisao text not null default 'Pendente'
);

create table acao_todo (
  id uuid primary key default gen_random_uuid(),
  cliente_id uuid not null references cliente(id) on delete cascade,
  conversa_id uuid references conversa(id) on delete cascade,
  descricao text not null,
  data_opcional date,
  concluida boolean not null default false
);

create index on conversa (cliente_id, data desc);
create index on informacao_insight (cliente_id, status_de_revisao);
create index on acao_todo (concluida);
