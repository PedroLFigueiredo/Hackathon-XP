# Roteiro de Execução — MVP Memória de Relacionamento

A execução tem três fases. A base é feita por um Claude só; depois dois Claudes trabalham em paralelo, cada um com o seu arquivo; no fim um deles integra.

| Fase | Quem | Arquivo | Tempo |
|---|---|---|---|
| Base (etapas 1 a 5) | 1 Claude | este arquivo | 2h30 |
| Paralelo | Claude do back | [EXECUCAO-BACK.md](EXECUCAO-BACK.md) | 2h00 |
| Paralelo | Claude do front | [EXECUCAO-FRONT.md](EXECUCAO-FRONT.md) | 2h15 |
| Integração (etapa 6) | 1 Claude | este arquivo | 0h25 |

Total: cerca de 5h10, contra 7h10 em sequência. Os detalhes técnicos de cada etapa estão nos techplans [00](00-visao-geral.md) a [04](04-telas.md). O banco é o `schema.sql` da raiz, feito pelo time.

## Estado

- **Fase atual:** Base, etapa 0 (aguardando pré-requisitos)
- **Modelo Whisper escolhido:** a definir na etapa 2
- **Observações:** —

## Regras de execução (valem para as três fases)

1. Uma etapa por vez, na ordem. Só avança quando o "Pronto quando" foi verificado rodando a aplicação, não só compilando.
2. Ao terminar uma etapa: marcar as caixas, atualizar o bloco **Estado** do próprio arquivo e fazer um commit `etapa N: <título>`.
3. Toda etapa termina com `dotnet build` sem erros e a aplicação subindo.
4. Problema de hardware com o Whisper é avisado na hora, não contornado em silêncio.
5. Segredos só no `.env` da raiz, que está fora do git. Nada de chave em arquivo versionado.
6. O `schema.sql` não é alterado sem a pessoa autorizar. Faltou coluna? Para e avisa.
7. Se uma etapa estourar o dobro do tempo previsto, parar e aplicar a linha de corte do [00](00-visao-geral.md).
8. Itens marcados **[VOCÊ]** dependem de uma pessoa.

## Pré-requisitos **[VOCÊ]**

- [x] Configurar a identidade do Git
- [x] Criar o projeto no Supabase (a connection string do Session pooler já está no `.env`)
- [ ] No `.env`: trocar `[YOUR-PASSWORD]` pela senha do banco
- [ ] No `.env`: preencher `OPENAI_API_KEY`
- [ ] Autorizar a instalação do .NET 8 SDK (`winget install Microsoft.DotNet.SDK.8`)
- [ ] Confirmar a decisão D1 (Npgsql + EF Core em vez do SDK do Supabase)
- [ ] Dizer se as tabelas já foram criadas no Supabase. A etapa 3 roda `--init-db`, que apaga e recria as seis tabelas a partir do `schema.sql`.

---

# Fase Base

Objetivo da base: deixar o ciclo completo funcionando com implementações falsas. Quando ela termina, colar um texto já cria conversa, passa pelos status e grava itens fictícios nas tabelas. A partir daí o back só troca os fakes pelos serviços reais e o front só constrói tela, sem um esperar o outro.

## Etapa 1 — Setup do projeto (20 min)

Plano: [01](01-fundacao-e-dados.md), seção Setup.

- [ ] Instalar o .NET 8 SDK e conferir `dotnet --list-sdks`
- [ ] `dotnet new webapp -n MemoriaRelacionamento -f net8.0`
- [ ] Conferir o `.gitignore` da raiz
- [ ] Pastas `Data/`, `Models/`, `Services/`, `db/`, `models/`, `uploads/`

**Pronto quando:** `dotnet run` sobe e a página padrão responde 200.

## Etapa 2 — Spike do Whisper (15 min)

Plano: [02](02-ingestao-audio-e-texto.md), seção Spike. Feito cedo para o risco de hardware aparecer antes de tudo.

- [ ] Baixar `ggml-small.bin` e `ggml-base.bin` para `models/`
- [ ] Gerar um áudio de teste em português de ~1 min com a voz do Windows (`System.Speech`) a partir de uma transcrição fictícia, salvo em `uploads/teste.wav`. Se não houver voz pt-BR instalada: **[VOCÊ]** grava 1 minuto no celular
- [ ] Console descartável (fora do projeto principal) medindo carga do modelo e tempo de transcrição com `small` e com `base`
- [ ] Registrar os tempos e o modelo escolhido no bloco **Estado**

**Pronto quando:** existe um número medido (segundos de transcrição por minuto de áudio) para cada modelo e a escolha está registrada. Regra: `small` se levar até ~1,5× a duração do áudio; senão `base`.

## Etapa 3 — Dados, contratos e fakes (45 min)

Plano: [01](01-fundacao-e-dados.md); contratos no [00](00-visao-geral.md).

- [ ] Pacotes `DotNetEnv`, `Npgsql.EntityFrameworkCore.PostgreSQL` e `EFCore.NamingConventions`
- [ ] Carga do `.env` e conversão da `DATABASE_URL` para o formato do Npgsql; falha clara no startup se a senha não foi preenchida
- [ ] Seis entidades (`Cliente`, `Conversa`, `PerfilPessoal`, `InformacaoEvento`, `InformacaoFinanceiro`, `Acao`), enums e a interface `IRevisavel`
- [ ] `AppDbContext`: snake_case, nomes de tabela explícitos, enums em minúsculas, `Tags` como `text[]`, `conversas.data` em UTC
- [ ] `db/seed.sql` conforme o plano 01 (itens aprovados e pendentes nas três tabelas, aniversário próximo, conversa em `erro`, cliente sem conversa, tarefas e pautas em `acoes`)
- [ ] Comando `dotnet run -- --init-db`: apaga as seis tabelas, roda o `schema.sql` da raiz e o `db/seed.sql`, e encerra
- [ ] `ITranscriptionService`, `IConversationAnalysisService` e os records
- [ ] `FakeTranscriptionService` e `FakeConversationAnalysisService`, cada um com 3 s de atraso, escolhidos pelas flags `Fakes:Transcricao` e `Fakes:Analise` (ambas `true` no `appsettings.Development.json`)

**Pronto quando:** `--init-db` cria as seis tabelas no Supabase e uma consulta no startup lê os clientes do seed, incluindo um item com tags e uma conversa com status, sem erro de mapeamento.

## Etapa 4 — Clientes (20 min)

Plano: [01](01-fundacao-e-dados.md).

- [ ] Página `/Clientes`: lista + formulário de novo cliente
- [ ] Página `/Clientes/Detalhe/{id}` só com nome e contato
- [ ] Link no menu

**Pronto quando:** um POST no formulário cria o cliente, ele aparece na lista e o link abre o detalhe.

## Etapa 5 — Pipeline completo com fakes (50 min)

Planos: [02](02-ingestao-audio-e-texto.md), Parte 1; mapeamento no [03](03-analise-ia.md).

- [ ] `ConversaQueue`, `ConversaProcessor`, `ConversaWorker`
- [ ] Mapeamento do resultado da análise para `conversas`, `clientes.personalidade`, `clientes.perfil_financeiro`, `perfil_pessoal`, `informacao_eventos`, `informacao_financeiro` e `acoes` (pautas entram em `acoes` com a tag `pauta`), tudo num único `SaveChanges`
- [ ] Recuperação no startup das conversas presas, desligável por `Pipeline:RecuperarNoStartup`
- [ ] `ConversaService`, o ponto de contato único entre tela e pipeline:
  ```csharp
  Task<Guid> CriarDeTextoAsync(Guid clienteId, DateOnly data, string texto, CancellationToken ct);
  Task<Guid> CriarDeAudioAsync(Guid clienteId, DateOnly data, Stream arquivo, string nomeArquivo, CancellationToken ct);
  ```
- [ ] `GET /api/conversas/{id}/status` devolvendo `{ status, mensagemErro }`
- [ ] No detalhe do cliente, um formulário cru (sem estilo) com data, `textarea` e campo de arquivo, chamando os dois métodos do `ConversaService`

**Pronto quando:** pelo formulário cru, um texto chega a `concluido` sem passar por `transcrevendo`, e um arquivo de áudio passa por `transcrevendo` → `analisando` → `concluido`. Em ambos os casos os itens do fake aparecem nas quatro tabelas de destino e `clientes.personalidade` é atualizada.

## Bifurcação

Depois do commit da etapa 5, **os contratos ficam congelados**: interfaces, records, entidades, o `schema.sql` e as assinaturas do `ConversaService`.

- [ ] Commit da etapa 5 feito na `main`
- [ ] Criar as duas frentes e levar o `.env` para a pasta nova (ele não vai pelo git):
  ```
  git switch -c back
  git worktree add ../Hackathon-XP-front -b front
  Copy-Item .env ../Hackathon-XP-front/.env
  ```
- [ ] **[VOCÊ]** Abrir dois terminais do Claude Code:
  - em `C:\Users\figue\Hackathon-XP` → "execute o `techplans/EXECUCAO-BACK.md`"
  - em `C:\Users\figue\Hackathon-XP-front` → "execute o `techplans/EXECUCAO-FRONT.md`"

### Regras do paralelo

| Assunto | Regra |
|---|---|
| Pastas | Back: `Services/`, `Program.cs`, `.csproj`, `db/`. Front: `Pages/`, `wwwroot/`. Ninguém edita a pasta do outro. |
| Contratos | Precisa mudar um contrato congelado? Para e avisa a pessoa. Não muda sozinho. |
| Porta | Back em `http://localhost:5000`, front em `http://localhost:5080` (`dotnet run --urls ...`). |
| Banco | Os dois usam o mesmo Supabase. Ninguém roda `--init-db` durante o paralelo sem a pessoa autorizar, porque apaga os dados do outro. |
| Fakes | O front roda sempre com os dois fakes ligados. O back desliga os fakes pela linha de comando (`--Fakes:Analise=false`), sem editar o `appsettings`. |
| Roteiro | Cada Claude atualiza só o **Estado** do seu próprio arquivo. |

A pasta `models/` só existe na pasta do back, e o front não precisa dela.

---

# Fase de Integração

## Etapa 6 — Integração e ensaio (25 min)

Feita na pasta principal, quando as duas frentes terminarem.

- [ ] `git switch main`, `git merge back`, `git merge front`. Com a divisão de pastas respeitada não deve haver conflito.
- [ ] `git worktree remove ../Hackathon-XP-front`
- [ ] Trocar `Fakes:Transcricao` e `Fakes:Analise` para `false` no `appsettings.Development.json`
- [ ] `--init-db` para voltar ao seed limpo
- [ ] Roteiro da demo de ponta a ponta com os serviços reais: painel → cliente → colar texto → revisar e aprovar → enviar áudio → painel com tarefas, pautas e datas novas
- [ ] `README.md` na raiz: como instalar, baixar o modelo, preencher o `.env` e rodar
- [ ] **[VOCÊ]** Ensaio final com a apresentação

**Pronto quando:** partindo de um banco recém-criado, o roteiro completo roda sem erro com Whisper e OpenAI de verdade.

Se o back não tiver terminado o Whisper quando o front acabar, integra-se o que houver: a demo segue com texto, e o fake de transcrição só é desligado quando o áudio estiver estável.
