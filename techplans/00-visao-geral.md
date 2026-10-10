# 00. Visão Geral dos Techplans — MVP Memória de Relacionamento

Lê primeiro este arquivo. Ele consolida a análise de `contexto/`, as decisões técnicas e os contratos que os outros planos assumem.

A sequência de execução está em [EXECUCAO.md](EXECUCAO.md): uma base feita por um Claude, depois duas trilhas em paralelo ([back](EXECUCAO-BACK.md) e [front](EXECUCAO-FRONT.md)) e uma etapa de integração.

**O modelo de dados é o `schema.sql` da raiz do repositório.** Ele é a fonte de verdade; os planos se adaptam a ele, não o contrário.

> **Divergência em aberto.** O código que está na `main` não usa este schema. Ele roda sobre `MemoriaRelacionamento/db/schema.sql`, com quatro tabelas (`cliente`, `conversa`, `informacao_insight`, `acao_todo`), `crm_id` no cliente e sem coluna de revisão. O `schema.sql` da raiz tem seis tabelas, não tem `crm_id` e nenhum código o usa ainda. O time precisa escolher um dos dois antes de implementar a análise real; estes planos descrevem o da raiz.

| Plano | Entrega | Estimativa |
|---|---|---|
| [01 — Fundação e dados](01-fundacao-e-dados.md) | Projeto rodando, mapeamento do schema, CRUD de clientes, seed | 1h15 |
| [02 — Ingestão: áudio e texto](02-ingestao-audio-e-texto.md) | Duas entradas, um pipeline em background, Whisper offline | 1h45 |
| [03 — Análise por IA](03-analise-ia.md) | Transcrição → JSON estrito → perfil, eventos, financeiro, ações | 1h00 |
| [04 — Telas](04-telas.md) | Perfil do cliente, painel do assessor, polimento da demo | 2h40 |

Em sequência, com o setup, dá cerca de 7h10. Em paralelo, cerca de 5h10. A linha de corte está no fim deste arquivo.

## Fluxo com as duas entradas

A transcrição pode já existir. Então a conversa entra por um de dois caminhos e ambos convergem no mesmo campo (`conversas.transcricao`) antes da análise:

```
Entrada A: áudio (wav/mp3/m4a) ─► normaliza p/ 16 kHz mono ─► Whisper.net (offline) ─┐
                                                                                    ├─► conversas.transcricao ─► análise IA ─► itens pendentes
Entrada B: texto (colado ou .txt) ──────────────────────────────────────────────────┘                                        de revisão + ações
```

A regra do worker é uma só: **só transcreve se `transcricao` estiver vazia e houver `caminho_audio`**. Daí saem três coisas de graça:

- a entrada por texto não precisa de código próprio no pipeline, só pula a etapa;
- se a análise falhar, tentar de novo não roda o Whisper outra vez;
- a demo tem um plano B: se o Whisper estiver lento na hora, cola-se o texto.

Não há coluna de origem: conversa com `caminho_audio` nulo veio por texto.

## Avisos do ambiente (verificados nesta máquina)

1. **Não há .NET SDK instalado**, só runtimes (ASP.NET Core 8.0.21). Primeiro passo: `winget install Microsoft.DotNet.SDK.8`.
2. **Whisper vai rodar só em CPU.** A máquina é um i5-1334U (10 núcleos, linha U de baixo consumo), 16 GB de RAM, sem GPU dedicada. Minha estimativa, ainda não medida: `base` roda bem mais rápido que tempo real, `small` fica perto do tempo real, `medium` e `large` são inviáveis para demo ao vivo. O spike do plano 02 mede isso antes de qualquer outra coisa.
3. **Não há ffmpeg.** A conversão de áudio fica com o NAudio, que resolve dentro do processo no Windows.
4. Os segredos ficam no `.env` da raiz, que está no `.gitignore`. O `.env.example` é o modelo versionado.

## Decisões

| # | Decisão | Motivo |
|---|---|---|
| D1 | Acessar o Supabase como Postgres, com **Npgsql + EF Core**, em vez do SDK `supabase-csharp` | O SDK é mantido pela comunidade, obriga os modelos a herdar de `BaseModel` e passa pela API REST com RLS. EF Core é o caminho mais conhecido em C# e o dado continua no Supabase. **É o único desvio da stack do `contexto/02`; confirma com o time.** |
| D2 | `net8.0`, um único projeto Razor Pages | O runtime 8 já está na máquina. Sem camadas nem microsserviços. |
| D3 | Modelo Whisper `ggml-small` (fallback `ggml-base`), baixado manualmente para `models/` | Português com `base` erra nomes próprios, que são justamente o que queremos extrair. A app nunca baixa modelo em runtime: se o arquivo não existir, falha no startup com mensagem clara. |
| D4 | Aceitar WAV, MP3 e M4A, sempre convertendo para 16 kHz mono | O Whisper.net só aceita WAV 16 kHz. Um WAV comum (44,1 kHz estéreo) falharia, e ele pesa ~10 MB por minuto. |
| D5 | Structured Outputs (`json_schema` com `strict: true`) em vez de `json_object` | `json_object` garante JSON válido, mas não o formato. Com schema estrito o parse nunca quebra. Atende a regra de "JSON estrito" do `contexto/02`. |
| D6 | Fila em memória: `Channel<Guid>` + um `BackgroundService`, um job por vez | Whisper ocupa a CPU inteira; paralelizar só piora. No startup, conversas presas em status intermediário voltam para a fila. |
| D7 | Construir o caminho de **texto primeiro**, o áudio depois | O texto exercita o pipeline inteiro sem o risco do Whisper. O áudio vira só uma etapa a mais na frente. |
| D8 | Segredos em `.env` na raiz, carregado com o pacote `DotNetEnv` | Um arquivo só para o time copiar. `DATABASE_URL` fica no formato URI do Supabase e a app converte para o formato do Npgsql. |

## Como o `schema.sql` atende as telas

| Necessidade | Onde fica no schema |
|---|---|
| Tom da última reunião | `conversas.tom` |
| Estilo do cliente na conversa | `clientes.personalidade`, estimada pela IA e atualizada quando uma conversa conclui |
| Perfil de investidor | `clientes.perfil_financeiro`. A IA só registra o perfil quando ele é dito na conversa; não deduz (ver "Pontos para o pitch") |
| Resumo em 3 bullets | `conversas.resumo`, um bullet por linha |
| Família, hobbies, estilo de vida | `perfil_pessoal`, agrupado pela primeira tag |
| Datas importantes e próximos eventos | `informacao_eventos.data_evento`; a tag `anual` marca o que se repete todo ano (aniversários) |
| O que o cliente disse sobre dinheiro | `informacao_financeiro` (seção nova na tela do cliente, não existia no `contexto/03`) |
| To-dos | `acoes` com `status_conclusao` `pendente` ou `concluido` |
| Pautas sugeridas pela IA | `acoes` com a tag `pauta`. São lembretes para o assessor, então cabem na mesma tabela sem coluna nova |
| Revisão antes de entrar no perfil | `status_revisao` nas três tabelas de informação: `pendente`, `aprovado`, `descartado` |
| Entrada por texto | `caminho_audio` nulo |

### Ajustes feitos no `schema.sql`

Só os dois sem os quais o MVP não funciona (reaplicados na versão que trouxe `perfil_financeiro` e `personalidade`):

1. Faltava a vírgula depois de `email TEXT` em `clientes`; o script nem rodava.
2. `conversas.caminho_audio` deixou de ser `NOT NULL`, porque a conversa que entra por texto não tem áudio.

Se as tabelas já foram criadas no Supabase com a versão anterior: `ALTER TABLE conversas ALTER COLUMN caminho_audio DROP NOT NULL;`

### Sugestões não aplicadas

`clientes` não tem `crm_id`. A carga de `/Demo` e a sincronização por CSV que já estão no código identificam o cliente por essa coluna, com unicidade; sem ela, essas duas funções não rodam sobre este schema.

`informacao_eventos` e `informacao_financeiro` não têm `conversa_id` (a `perfil_pessoal` tem). O MVP funciona sem, mas não dá para saber de qual reunião veio um evento nem limpar os pendentes de uma conversa ao reprocessá-la. Por isso o reprocessamento ficou fora do escopo.

## Valores fixos

Gravados em minúsculas e sem acento, como nos comentários do schema:

- `conversas.status`: `aguardando`, `transcrevendo`, `analisando`, `concluido`, `erro`
- `tom`: `satisfeito`, `alinhado`, `neutro`, `atencao`
- `clientes.personalidade`: `analitico`, `comunicativo`, `impetuoso`, `reservado`
- `clientes.perfil_financeiro`: `conservador`, `moderado`, `arrojado`
- `status_revisao`: `pendente`, `aprovado`, `descartado`
- `acoes.status_conclusao`: `pendente`, `concluido`

Tags: a primeira tag de cada item vem de uma lista fixa, para a tela agrupar; as seguintes são livres.

| Tabela | Primeira tag |
|---|---|
| `perfil_pessoal` | `familia`, `hobby`, `estilo_de_vida`, `profissional`, `objetivo`, `outro` |
| `informacao_eventos` | `anual` (aniversários e datas que se repetem) ou `pontual` |
| `informacao_financeiro` | `objetivo`, `patrimonio`, `renda`, `liquidez`, `preocupacao`, `outro` |
| `acoes` | `pauta` (gancho de conversa sugerido pela IA) ou `tarefa` |

## Contratos compartilhados

Definidos na fase base, junto com implementações falsas. Assim quem faz tela não espera Whisper nem OpenAI.

```csharp
public interface ITranscriptionService
{
    Task<string> TranscreverAsync(string caminhoAudio, CancellationToken ct);
}

public interface IConversationAnalysisService
{
    Task<AnaliseConversa> AnalisarAsync(AnaliseRequest request, CancellationToken ct);
}

public record AnaliseRequest(
    string NomeCliente,
    DateOnly DataConversa,
    string Transcricao,
    IReadOnlyList<string> FatosJaConhecidos);

public record AnaliseConversa(
    List<string> Resumo,
    Tom Tom,
    Personalidade? Personalidade,
    PerfilFinanceiro? PerfilFinanceiro,
    List<InfoExtraida> PerfilPessoal,
    List<EventoExtraido> Eventos,
    List<InfoExtraida> Financeiro,
    List<AcaoExtraida> Acoes,
    List<string> PautasSugeridas);

public record InfoExtraida(string Conteudo, string TrechoOrigem, List<string> Tags);
public record EventoExtraido(string Conteudo, DateOnly? DataEvento, string TrechoOrigem, List<string> Tags);
public record AcaoExtraida(string Descricao, DateOnly? Data, List<string> Tags);
```

Enums em C#, convertidos para minúsculas ao gravar:

- `StatusConversa`: `Aguardando`, `Transcrevendo`, `Analisando`, `Concluido`, `Erro`
- `Tom`: `Satisfeito`, `Alinhado`, `Neutro`, `Atencao`
- `Personalidade`: `Analitico`, `Comunicativo`, `Impetuoso`, `Reservado`
- `PerfilFinanceiro`: `Conservador`, `Moderado`, `Arrojado`
- `StatusRevisao`: `Pendente`, `Aprovado`, `Descartado`
- `StatusConclusao`: `Pendente`, `Concluido`

## Linha de corte

| Prioridade | Itens |
|---|---|
| **Precisa ter** | Cliente, entrada por texto, análise, revisão (aprovar/descartar), perfil do cliente, entrada por áudio com Whisper |
| **Deveria ter** | Painel com to-dos e datas, edição na revisão, linha do tempo, dedupe via fatos conhecidos, MP3/M4A |
| **Se sobrar tempo** | Pautas na Home, contagem regressiva, drag-and-drop, upload de `.txt` |

Se às 5h o áudio ainda não estiver estável, a demo segue com texto e um áudio curto já processado no seed.

## Pontos para o pitch

**Privacidade.** A transcrição é offline, mas o texto vai para a OpenAI. Para dados de cliente de corretora isso vai ser perguntado. Resposta para a banca: a demo usa dados fictícios, e a análise está atrás de `IConversationAnalysisService`, então trocar por um modelo local ou por um endpoint corporativo é troca de uma classe. Mascarar CPF e número de conta antes do envio fica como próximo passo.

**Informação financeira sem recomendação.** A tabela `informacao_financeiro` registra o que o cliente disse (objetivos, preocupações, patrimônio citado). O sistema não classifica perfil de risco nem sugere investimento, que continuam fora do escopo do `contexto/01`. O prompt do [03](03-analise-ia.md) deixa isso explícito.

**`perfil_financeiro` não é suitability.** Conservador, moderado e arrojado são as categorias do perfil de investidor, que numa corretora sai de um questionário regulado, não de uma conversa. Se a IA deduzisse esse campo, o MVP faria exatamente a "classificação automática de perfil de risco" que o `contexto/01` excluiu. Por isso a IA só preenche o campo quando o perfil é dito com todas as letras na reunião, e devolve `null` no resto. Se o time quiser que a IA estime, é uma mudança de escopo consciente, e a tela precisa rotular como "percepção da conversa, não substitui o suitability". `personalidade` é diferente: é estilo de comunicação, vai rotulada como "estimada pela IA".
