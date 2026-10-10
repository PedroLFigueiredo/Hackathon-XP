# 01. Fundação e Dados

**Objetivo:** projeto rodando, o `schema.sql` do time mapeado no EF Core, clientes cadastráveis e os contratos do [00](00-visao-geral.md) no código, com implementações falsas para destravar as outras trilhas.

**Estimativa:** 30 min de setup + 1h15.

## Setup (30 min)

- [ ] `winget install Microsoft.DotNet.SDK.8` e conferir com `dotnet --list-sdks`
- [ ] No `.env` da raiz: trocar `[YOUR-PASSWORD]` pela senha do banco e preencher `OPENAI_API_KEY`
- [ ] `dotnet new webapp -n MemoriaRelacionamento -f net8.0` na raiz do repo
- [ ] Conferir o `.gitignore` da raiz (já cobre `.env`, `models/`, `uploads/`, `bin/`, `obj/`)
- [ ] Spike do Whisper (15 min, detalhado no [02](02-ingestao-audio-e-texto.md)) para medir a velocidade antes de se comprometer com o modelo

## Estrutura

```
Hackathon-XP/
  .env                 segredos (fora do git)
  .env.example         modelo versionado
  schema.sql           fonte de verdade do banco (feito pelo time)
  contexto/  techplans/
  MemoriaRelacionamento/
    Program.cs
    Data/AppDbContext.cs
    Models/            entidades e enums
    Services/          contratos, fakes, Whisper, OpenAI, fila e worker
    Pages/             Index, Clientes/Index, Clientes/Detalhe
    db/seed.sql
    models/            modelo ggml (fora do git)
    uploads/           áudios enviados (fora do git)
```

## Configuração e segredos

- [ ] Pacote `DotNetEnv`. No começo do `Program.cs`: `Env.TraversePaths().Load();`, que procura o `.env` subindo as pastas a partir do diretório atual
- [ ] Converter `DATABASE_URL` (formato URI) para o formato do Npgsql, que não aceita URI:
  ```csharp
  var uri = new Uri(Environment.GetEnvironmentVariable("DATABASE_URL")!);
  var credenciais = uri.UserInfo.Split(':', 2);
  var connectionString = new NpgsqlConnectionStringBuilder
  {
      Host = uri.Host,
      Port = uri.Port,
      Database = uri.AbsolutePath.TrimStart('/'),
      Username = credenciais[0],
      Password = Uri.UnescapeDataString(credenciais[1]),
      SslMode = SslMode.Require,
  }.ConnectionString;
  ```
- [ ] `OPENAI_API_KEY` lida da variável de ambiente
- [ ] Sem `DATABASE_URL`, ou ainda com `[YOUR-PASSWORD]`, falhar no startup com mensagem dizendo o que preencher no `.env`
- [ ] Configuração que não é segredo fica no `appsettings.Development.json`: `Fakes:Transcricao`, `Fakes:Analise`, `Whisper:ModelPath`, `Pipeline:RecuperarNoStartup`

## Mapeamento do schema

O DDL está em `schema.sql` na raiz e não é repetido aqui. Sem migrations do EF: o EF só mapeia.

| Tabela | Entidade | Observações |
|---|---|---|
| `clientes` | `Cliente` | `personalidade` e `perfil_financeiro` são texto, opcionais, preenchidos pela análise |
| `conversas` | `Conversa` | `data` é `timestamptz`; `caminho_audio` nulo = entrada por texto; `resumo` é texto com um bullet por linha |
| `perfil_pessoal` | `PerfilPessoal` | tem `conversa_id` |
| `informacao_eventos` | `InformacaoEvento` | tem `data_evento`; sem `conversa_id` |
| `informacao_financeiro` | `InformacaoFinanceiro` | sem `conversa_id` |
| `acoes` | `Acao` | `status_conclusao` é texto; `conversa_id` obrigatório |

## Passos

- [ ] Pacotes: `Npgsql.EntityFrameworkCore.PostgreSQL` e `EFCore.NamingConventions` (versões 8.x)
- [ ] Entidades em `Models/`, com as propriedades em PascalCase batendo com as colunas (`CaminhoAudio`, `MensagemErro`, `TrechoOrigem`, `StatusRevisao`, `DataEvento`, `DataOpcional`, `StatusConclusao`). `Tags` é `List<string>`, que o Npgsql mapeia para `text[]`.
- [ ] Interface `IRevisavel` (`Id`, `ClienteId`, `Conteudo`, `TrechoOrigem`, `Tags`, `StatusRevisao`) implementada por `PerfilPessoal`, `InformacaoEvento` e `InformacaoFinanceiro`, para a revisão tratar as três tabelas com o mesmo código
- [ ] `AppDbContext` com `UseNpgsql(...).UseSnakeCaseNamingConvention()` e `ToTable(...)` explícito para as seis tabelas
- [ ] Enums gravados em minúsculas: um `ValueConverter` genérico (`v.ToString().ToLowerInvariant()` na ida, `Enum.Parse` ignorando maiúsculas na volta) aplicado a `StatusConversa`, `Tom`, `Personalidade`, `PerfilFinanceiro`, `StatusRevisao` e `StatusConclusao`
- [ ] `conversas.data` como `DateTime` sempre em UTC. O Npgsql recusa gravar em `timestamptz` um valor que não seja UTC.
- [ ] Comando `dotnet run -- --init-db`: apaga as seis tabelas, roda o `schema.sql` da raiz e o `db/seed.sql`, e encerra. É destrutivo; serve para criar o banco e para voltar ao seed limpo antes da demo.
- [ ] Contratos do [00](00-visao-geral.md) em `Services/`
- [ ] `FakeTranscriptionService` e `FakeConversationAnalysisService`, cada um com 3 s de atraso e resultado fixo, cobrindo as três tabelas de informação, uma ação com data e uma pauta. As flags `Fakes:Transcricao` e `Fakes:Analise` escolhem o registro no DI de cada um.
- [ ] Página `/Clientes`: lista com link para o perfil e formulário de novo cliente (nome obrigatório)
- [ ] `db/seed.sql` com dados fictícios, rico o bastante para o front trabalhar sozinho:
  - um cliente com conversa `concluido` e itens `aprovado` nas três tabelas, incluindo um aniversário (tag `anual`) nos próximos dias;
  - um cliente com conversa `concluido` e itens `pendente` nas três tabelas;
  - um cliente com conversa em `erro` e `mensagem_erro` preenchida;
  - um cliente sem nenhuma conversa;
  - em `acoes`: três tarefas pendentes, uma concluída e duas pautas (tag `pauta`).

## Critérios de aceite

- `dotnet run -- --init-db` cria as seis tabelas no Supabase e carrega o seed
- `dotnet run` sobe sem erro e `/Clientes` lista os clientes do seed
- Criar um cliente pela tela grava no Supabase (conferir no Table Editor)
- Com as duas flags de fake em `true`, a app sobe sem modelo Whisper e sem chave da OpenAI

## Riscos

| Risco | Mitigação |
|---|---|
| Senha com caractere especial quebra a `DATABASE_URL` | Usar URL-encoding na senha (o `.env` tem o lembrete) ou trocar a senha do banco por uma só com letras e números |
| Conexão recusada ou timeout | Conferir que é a string do Session pooler (`pooler.supabase.com:5432`); a conexão direta é só IPv6 |
| Coluna não encontrada | Nome da propriedade não bate com a coluna em snake_case. Ligar o log de SQL do EF em Development. |
| Erro ao ler ou gravar enum | Faltou o conversor de minúsculas; sem ele o EF tenta gravar inteiro em coluna `text` |
| `Cannot write DateTime with Kind=Local` | `conversas.data` precisa estar em UTC |
