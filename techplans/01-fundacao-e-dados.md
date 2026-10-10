# 01. Fundação e Dados

**Objetivo:** projeto rodando, banco criado, clientes cadastráveis e os contratos do [00](00-visao-geral.md) no código, com implementações falsas para destravar as outras trilhas.

**Estimativa:** 30 min de setup + 1h15.

## Setup (30 min)

- [ ] `winget install Microsoft.DotNet.SDK.8` e conferir com `dotnet --list-sdks`
- [ ] Criar o projeto Supabase e copiar a connection string do **Session pooler** (porta 5432). A conexão direta do Supabase é só IPv6 e costuma falhar em rede comum.
- [ ] Criar a chave da OpenAI
- [ ] `dotnet new webapp -n MemoriaRelacionamento -f net8.0` na raiz do repo
- [ ] `.gitignore` (`dotnet new gitignore`) acrescido de `models/` e `uploads/`
- [ ] Segredos:
  ```
  dotnet user-secrets init
  dotnet user-secrets set "ConnectionStrings:Supabase" "Host=...;Port=5432;Database=postgres;Username=postgres.xxxx;Password=...;SSL Mode=Require"
  dotnet user-secrets set "OpenAI:ApiKey" "sk-..."
  ```
- [ ] Spike do Whisper (15 min, detalhado no [02](02-ingestao-audio-e-texto.md)) para medir a velocidade antes de se comprometer com o modelo

## Estrutura do projeto

```
MemoriaRelacionamento/
  Program.cs
  Data/AppDbContext.cs
  Models/            Cliente, Conversa, InformacaoInsight, AcaoToDo, enums
  Services/          contratos, fakes, Whisper, OpenAI, fila e worker
  Pages/             Index, Clientes/Index, Clientes/Detalhe
  db/schema.sql
  db/seed.sql
  models/            modelo ggml (fora do git)
  uploads/           áudios enviados (fora do git)
```

## Schema (`db/schema.sql`)

Sem migrations do EF: o schema é este arquivo, o EF só mapeia. O comando `dotnet run -- --init-db` aplica `schema.sql` e `seed.sql` pelo Npgsql e encerra; colar no SQL Editor do Supabase também funciona.

```sql
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
```

Diferenças em relação ao `contexto/02`: `origem`, `pautas_sugeridas`, `data_referencia`, `recorrente_anual`, `criado_em`; `resumo` virou array; `status_de_conclusao` virou o booleano `concluida`.

## Passos

- [ ] Pacotes: `Npgsql.EntityFrameworkCore.PostgreSQL` e `EFCore.NamingConventions` (versões 8.x)
- [ ] Entidades e enums em `Models/`. `DateOnly` para datas, `List<string>` para os arrays.
- [ ] `AppDbContext` com `UseNpgsql(...).UseSnakeCaseNamingConvention()`, nomes de tabela explícitos (`ToTable("cliente")` etc.) e `HasConversion<string>()` em todos os enums
- [ ] Comando `--init-db` em `Program.cs`; rodar e validar com um `db.Clientes.CountAsync()` no startup
- [ ] Contratos do [00](00-visao-geral.md) em `Services/`
- [ ] `FakeTranscriptionService` (devolve um texto fixo depois de 3 s) e `FakeConversationAnalysisService` (devolve uma `AnaliseConversa` fixa). Duas flags no `appsettings.Development.json`, `Fakes:Transcricao` e `Fakes:Analise`, escolhem o registro no DI de cada um.
- [ ] Página `/Clientes`: lista com link para o perfil e formulário de novo cliente (nome obrigatório)
- [ ] `db/seed.sql`: 3 clientes fictícios, um deles com uma conversa `Concluida`, insights aprovados (incluindo um aniversário nos próximos dias) e 2 to-dos pendentes

## Critérios de aceite

- `dotnet run` sobe sem erro e `/Clientes` lista os clientes do seed
- Criar um cliente pela tela grava no Supabase (conferir no Table Editor)
- Com as duas flags de fake em `true`, a app sobe sem modelo Whisper e sem chave da OpenAI

## Riscos

| Risco | Mitigação |
|---|---|
| Conexão recusada ou timeout | Quase sempre é a connection string direta (IPv6). Usar a do Session pooler. |
| Coluna não encontrada | Nome em snake_case divergente. Conferir o SQL gerado ligando o log do EF em Development. |
| Erro ao gravar enum | Faltou `HasConversion<string>()`; sem ele o EF grava inteiro em coluna `text`. |
