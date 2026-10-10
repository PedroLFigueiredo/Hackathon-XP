# Roteiro de Execução — MVP Memória de Relacionamento

A execução tem três fases. A base é feita por um Claude só; depois dois Claudes trabalham em paralelo, cada um com o seu arquivo; no fim um deles integra.

| Fase | Quem | Arquivo | Tempo |
|---|---|---|---|
| Base (etapas 1 a 5) | 1 Claude | este arquivo | 2h30 |
| Paralelo | Claude do back | [EXECUCAO-BACK.md](EXECUCAO-BACK.md) | 2h00 |
| Paralelo | Claude do front | [EXECUCAO-FRONT.md](EXECUCAO-FRONT.md) | 2h05 |
| Integração (etapa 6) | 1 Claude | este arquivo | 0h25 |

Total: cerca de 5h, contra 6h55 em sequência. Os detalhes técnicos de cada etapa estão nos techplans [00](00-visao-geral.md) a [04](04-telas.md).

## Estado

- **Fase atual:** Base, etapa 0 (aguardando pré-requisitos)
- **Modelo Whisper escolhido:** a definir na etapa 2
- **Observações:** —

## Regras de execução (valem para as três fases)

1. Uma etapa por vez, na ordem. Só avança quando o "Pronto quando" foi verificado rodando a aplicação, não só compilando.
2. Ao terminar uma etapa: marcar as caixas, atualizar o bloco **Estado** do próprio arquivo e fazer um commit `etapa N: <título>`.
3. Toda etapa termina com `dotnet build` sem erros e a aplicação subindo.
4. Problema de hardware com o Whisper é avisado na hora, não contornado em silêncio.
5. Segredos só em `dotnet user-secrets`. Nada de chave em arquivo versionado.
6. Se uma etapa estourar o dobro do tempo previsto, parar e aplicar a linha de corte do [00](00-visao-geral.md).
7. Itens marcados **[VOCÊ]** dependem de uma pessoa.

## Pré-requisitos **[VOCÊ]**

- [ ] Configurar a identidade do Git (`git config --global user.name` e `user.email`)
- [ ] Autorizar a instalação do .NET 8 SDK (`winget install Microsoft.DotNet.SDK.8`)
- [ ] Criar o projeto no Supabase e copiar a connection string do **Session pooler** (porta 5432)
- [ ] Criar a chave da OpenAI
- [ ] Confirmar a decisão D1 (Npgsql + EF Core em vez do SDK do Supabase)
- [ ] Depois da etapa 1, gravar os segredos no seu próprio terminal, dentro de `MemoriaRelacionamento/`:
  ```
  dotnet user-secrets set "ConnectionStrings:Supabase" "Host=...;Port=5432;Database=postgres;Username=postgres.xxxx;Password=...;SSL Mode=Require"
  dotnet user-secrets set "OpenAI:ApiKey" "sk-..."
  ```

---

# Fase Base

Objetivo da base: deixar o ciclo completo funcionando com implementações falsas. Quando ela termina, colar um texto já cria conversa, passa pelos status e gera insights fictícios. A partir daí o back só troca os fakes pelos serviços reais e o front só constrói tela, sem um esperar o outro.

## Etapa 1 — Setup do projeto (20 min)

Plano: [01](01-fundacao-e-dados.md), seção Setup.

- [ ] Instalar o .NET 8 SDK e conferir `dotnet --list-sdks`
- [ ] `dotnet new webapp -n MemoriaRelacionamento -f net8.0`
- [ ] `.gitignore` com `models/` e `uploads/`
- [ ] `dotnet user-secrets init`
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

- [ ] Pacotes `Npgsql.EntityFrameworkCore.PostgreSQL` e `EFCore.NamingConventions`
- [ ] Entidades, enums e `AppDbContext` (snake_case, enums como texto)
- [ ] `db/schema.sql`
- [ ] `db/seed.sql` rico o bastante para o front trabalhar sozinho: 3 clientes fictícios; um com conversa `Concluida` e insights `Aprovado` em todas as categorias, incluindo um aniversário nos próximos dias; um com conversa `Concluida` e insights `Pendente`; um com conversa em `Erro`; um cliente sem nenhuma conversa; 3 to-dos pendentes e 1 concluído
- [ ] Comando `dotnet run -- --init-db`: aplica `schema.sql` e `seed.sql` pelo Npgsql e encerra
- [ ] `ITranscriptionService`, `IConversationAnalysisService` e os records
- [ ] `FakeTranscriptionService` e `FakeConversationAnalysisService`, cada um com 3 s de atraso, escolhidos por duas flags separadas: `Fakes:Transcricao` e `Fakes:Analise` (ambas `true` no `appsettings.Development.json`)

**Pronto quando:** `--init-db` cria as 4 tabelas no Supabase e uma consulta de contagem no startup devolve os clientes do seed.

## Etapa 4 — Clientes (20 min)

Plano: [01](01-fundacao-e-dados.md).

- [ ] Página `/Clientes`: lista + formulário de novo cliente
- [ ] Página `/Clientes/Detalhe/{id}` só com nome e contato
- [ ] Link no menu

**Pronto quando:** um POST no formulário cria o cliente, ele aparece na lista e o link abre o detalhe.

## Etapa 5 — Pipeline completo com fakes (50 min)

Plano: [02](02-ingestao-audio-e-texto.md), Parte 1.

- [ ] `ConversaQueue`, `ConversaProcessor`, `ConversaWorker`
- [ ] Mapeamento do resultado da análise para `conversa`, `informacao_insight` e `acao_todo` (regras no [03](03-analise-ia.md))
- [ ] Recuperação no startup das conversas presas, desligável por `Pipeline:RecuperarNoStartup`
- [ ] `ConversaService`, o ponto de contato único entre tela e pipeline:
  ```csharp
  Task<Guid> CriarDeTextoAsync(Guid clienteId, DateOnly data, string texto, CancellationToken ct);
  Task<Guid> CriarDeAudioAsync(Guid clienteId, DateOnly data, Stream arquivo, string nomeArquivo, CancellationToken ct);
  ```
  O de áudio valida a extensão, salva em `uploads/{conversaId}{ext}` e enfileira.
- [ ] `GET /api/conversas/{id}/status` devolvendo `{ status, mensagemDeErro }`
- [ ] No detalhe do cliente, um formulário cru (sem estilo) com data, `textarea` e campo de arquivo, chamando os dois métodos do `ConversaService`

**Pronto quando:** pelo formulário cru, um texto chega a `Concluida` sem passar por `Transcrevendo`, e um arquivo de áudio passa por `Transcrevendo` → `Analisando` → `Concluida`. Em ambos os casos os insights e to-dos do fake aparecem no banco.

## Bifurcação

Depois do commit da etapa 5, **os contratos ficam congelados**: interfaces, records, entidades, schema e as assinaturas do `ConversaService`.

- [ ] Commit da etapa 5 feito na `main`
- [ ] Criar as duas frentes:
  ```
  git switch -c back
  git worktree add ../Hackathon-XP-front -b front
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

Os segredos do `user-secrets` valem para as duas pastas, porque ficam no perfil do usuário e não no repositório. A pasta `models/` só existe na pasta do back, e o front não precisa dela.

---

# Fase de Integração

## Etapa 6 — Integração e ensaio (25 min)

Feita na pasta principal, quando as duas frentes terminarem.

- [ ] `git switch main`, `git merge back`, `git merge front`. Com a divisão de pastas respeitada não deve haver conflito.
- [ ] `git worktree remove ../Hackathon-XP-front`
- [ ] Trocar `Fakes:Transcricao` e `Fakes:Analise` para `false` no `appsettings.Development.json`
- [ ] `--init-db` para voltar ao seed limpo
- [ ] Roteiro da demo de ponta a ponta com os serviços reais: painel → cliente → colar texto → revisar e aprovar → enviar áudio → painel com to-dos e datas novas
- [ ] `README.md` na raiz: como instalar, baixar o modelo, configurar segredos e rodar
- [ ] **[VOCÊ]** Ensaio final com a apresentação

**Pronto quando:** partindo de um banco recém-criado, o roteiro completo roda sem erro com Whisper e OpenAI de verdade.

Se o back não tiver terminado o Whisper quando o front acabar, integra-se o que houver: a demo segue com texto e o áudio fica com o fake desligado só quando estiver estável.
