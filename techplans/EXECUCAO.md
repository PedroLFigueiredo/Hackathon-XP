# Roteiro de Execução — MVP Memória de Relacionamento

Arquivo único com todas as etapas, na ordem em que o Claude Code executa. Os detalhes de cada etapa (schema, contratos, prompt, layout) estão nos techplans [00](00-visao-geral.md) a [04](04-telas.md); aqui fica a sequência, o que verifica cada etapa e o estado atual.

## Estado

- **Etapa atual:** 0 (aguardando pré-requisitos)
- **Modelo Whisper escolhido:** a definir na etapa 2
- **Observações:** —

## Regras de execução

1. Uma etapa por vez, na ordem. Só avança quando o "Pronto quando" da etapa foi verificado de fato (rodando a aplicação, não só compilando).
2. Ao terminar uma etapa: marcar as caixas, atualizar o bloco **Estado** e fazer um commit com a mensagem `etapa N: <título>`.
3. Toda etapa termina com `dotnet build` sem erros e a aplicação subindo.
4. Problema de hardware com o Whisper é avisado na hora, não contornado em silêncio.
5. Segredos só em `dotnet user-secrets`. Nada de chave em arquivo versionado.
6. Se uma etapa estourar o dobro do tempo previsto, parar e aplicar a linha de corte do [00](00-visao-geral.md).
7. Itens marcados **[VOCÊ]** dependem de uma pessoa; o resto o Claude faz sozinho.

## Pré-requisitos **[VOCÊ]**

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
- [ ] Gerar um áudio de teste em português de ~1 min com a voz do Windows (`System.Speech`) a partir de uma transcrição fictícia. Se não houver voz pt-BR instalada: **[VOCÊ]** grava 1 minuto no celular e coloca em `uploads/teste.m4a`
- [ ] Console descartável (fora do projeto principal) medindo carga do modelo e tempo de transcrição com `small` e com `base`
- [ ] Registrar os tempos e o modelo escolhido no bloco **Estado**

**Pronto quando:** existe um número medido (segundos de transcrição por minuto de áudio) para cada modelo e a escolha está registrada. Regra: `small` se levar até ~1,5× a duração do áudio; senão `base`.

## Etapa 3 — Dados, contratos e fakes (45 min)

Plano: [01](01-fundacao-e-dados.md); contratos no [00](00-visao-geral.md).

- [ ] Pacotes `Npgsql.EntityFrameworkCore.PostgreSQL` e `EFCore.NamingConventions`
- [ ] Entidades, enums e `AppDbContext` (snake_case, enums como texto)
- [ ] `db/schema.sql` e `db/seed.sql` (3 clientes fictícios, uma conversa concluída, um aniversário próximo, 2 to-dos)
- [ ] Comando `dotnet run -- --init-db`: aplica `schema.sql` e `seed.sql` pelo Npgsql e encerra
- [ ] `ITranscriptionService`, `IConversationAnalysisService` e os records
- [ ] `FakeTranscriptionService` e `FakeConversationAnalysisService`, escolhidos pela flag `UsarFakes`

**Pronto quando:** `--init-db` cria as 4 tabelas no Supabase e uma consulta de contagem no startup devolve os 3 clientes do seed.

## Etapa 4 — Clientes (20 min)

Plano: [01](01-fundacao-e-dados.md).

- [ ] Página `/Clientes`: lista + formulário de novo cliente
- [ ] Página `/Clientes/Detalhe/{id}` ainda vazia, só com nome e contato
- [ ] Link no menu

**Pronto quando:** um POST no formulário cria o cliente, ele aparece na lista e o link abre o detalhe.

## Etapa 5 — Pipeline em background com entrada de texto (45 min)

Plano: [02](02-ingestao-audio-e-texto.md), Parte 1.

- [ ] `ConversaQueue`, `ConversaProcessor`, `ConversaWorker`
- [ ] Recuperação no startup das conversas presas
- [ ] Handler `OnPostTextoAsync` no detalhe do cliente (data + texto)
- [ ] `GET /api/conversas/{id}/status`

**Pronto quando:** com `UsarFakes: true`, enviar um texto por `curl` cria a conversa e o endpoint de status passa por `Analisando` e chega a `Concluida`, sem passar por `Transcrevendo`. Os insights e to-dos do fake aparecem no banco.

## Etapa 6 — Análise real com a OpenAI (1h)

Plano: [03](03-analise-ia.md).

- [ ] Pacote `OpenAI`
- [ ] Schema JSON estrito e prompt de sistema
- [ ] `OpenAiConversationAnalysisService` com tratamento de recusa e truncamento
- [ ] `FatosJaConhecidos` e mapeamento do resultado no `ConversaProcessor`, limpando pendentes no reprocessamento
- [ ] Três transcrições fictícias em `db/transcricoes-exemplo/`

**Pronto quando:** com `UsarFakes: false` para a análise, a transcrição rica gera insights de família e uma ação, todos com `trecho_de_origem` presente no texto; a transcrição só de negócios não gera fatos pessoais inventados; chave inválida leva a conversa para `Erro` com mensagem legível.

## Etapa 7 — Tela do cliente (1h30)

Plano: [04](04-telas.md), Tela 2.

- [ ] Cartão "Nova conversa" com as abas Áudio e Texto (a aba Áudio fica desabilitada até a etapa 8)
- [ ] Status em andamento + polling de 2 s
- [ ] Seção 1: sentimento e resumo
- [ ] Seção 3: revisão de insights (aprovar, editar, descartar, aprovar todos)
- [ ] Seção 2: CRM humanizado só com aprovados + próximos eventos
- [ ] Linha do tempo e to-do local

**Pronto quando:** colar uma transcrição de exemplo na tela leva, sem recarregar à mão, até a lista de revisão; aprovar move o insight para o perfil, descartar some com ele, editar grava o texto novo.

## Etapa 8 — Entrada de áudio com Whisper (1h)

Plano: [02](02-ingestao-audio-e-texto.md), Parte 2.

- [ ] Pacotes `Whisper.net`, `Whisper.net.Runtime`, `NAudio`
- [ ] `WhisperFactory` singleton + checagem do modelo no startup
- [ ] `AudioNormalizer` (qualquer entrada → WAV 16 kHz mono)
- [ ] `WhisperTranscriptionService` com idioma `pt`
- [ ] Handler `OnPostAudioAsync`, limite de upload de 200 MB, aba Áudio habilitada com arrastar e soltar

**Pronto quando:** o áudio de teste da etapa 2 enviado pela tela passa por `Transcrevendo` → `Analisando` → `Concluida` com transcrição legível; um arquivo inválido vira `Erro` sem derrubar a aplicação; a transcrição funciona com a rede desligada.

## Etapa 9 — Painel do assessor (35 min)

Plano: [04](04-telas.md), Tela 1.

- [ ] Widget 1: datas importantes dos próximos 30 dias + pautas sugeridas
- [ ] Widget 2: to-dos pendentes de toda a carteira com checkbox
- [ ] Contador de insights aguardando revisão por cliente

**Pronto quando:** o aniversário do seed aparece com o cliente certo e marcar um to-do no painel reflete no perfil do cliente.

## Etapa 10 — Polimento e ensaio (25 min)

Plano: [04](04-telas.md), Polimento.

- [ ] Estados vazios e mensagens de erro revisados
- [ ] Seed final conferido (só dados fictícios)
- [ ] `README.md` na raiz: como instalar, baixar o modelo, configurar segredos e rodar
- [ ] Roteiro da demo executado do início ao fim uma vez, cronometrado
- [ ] **[VOCÊ]** Ensaio final com a apresentação

**Pronto quando:** partindo de um banco recém-criado com `--init-db`, o roteiro completo (painel → cliente → texto → revisão → áudio → painel) roda sem erro.

---

## Resumo de tempo

| Etapa | Tempo | Acumulado |
|---|---|---|
| 1. Setup | 0h20 | 0h20 |
| 2. Spike Whisper | 0h15 | 0h35 |
| 3. Dados, contratos e fakes | 0h45 | 1h20 |
| 4. Clientes | 0h20 | 1h40 |
| 5. Pipeline com texto | 0h45 | 2h25 |
| 6. Análise OpenAI | 1h00 | 3h25 |
| 7. Tela do cliente | 1h30 | 4h55 |
| 8. Áudio com Whisper | 1h00 | 5h55 |
| 9. Painel | 0h35 | 6h30 |
| 10. Polimento | 0h25 | 6h55 |

Ponto de checagem: ao fim da etapa 7 (≈5h) o MVP já é demonstrável só com texto. Se a etapa 8 travar, a demo segue com texto e um áudio já processado no seed.
