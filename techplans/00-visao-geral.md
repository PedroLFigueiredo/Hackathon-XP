# 00. Visão Geral dos Techplans — MVP Memória de Relacionamento

Lê primeiro este arquivo. Ele consolida a análise de `contexto/`, as decisões técnicas e os contratos que os outros planos assumem.

A sequência de execução está em [EXECUCAO.md](EXECUCAO.md): uma base feita por um Claude, depois duas trilhas em paralelo ([back](EXECUCAO-BACK.md) e [front](EXECUCAO-FRONT.md)) e uma etapa de integração.

| Plano | Entrega | Estimativa |
|---|---|---|
| [01 — Fundação e dados](01-fundacao-e-dados.md) | Projeto rodando, schema no Supabase, CRUD de clientes, seed | 1h15 |
| [02 — Ingestão: áudio e texto](02-ingestao-audio-e-texto.md) | Duas entradas, um pipeline em background, Whisper offline | 1h45 |
| [03 — Análise por IA](03-analise-ia.md) | Transcrição → JSON estrito → insights, to-dos, resumo | 1h00 |
| [04 — Telas](04-telas.md) | Perfil do cliente, painel do assessor, polimento da demo | 2h30 |

Somando o setup (30 min), dá as 7 horas sem folga. A linha de corte está no fim deste arquivo.

## Fluxo com as duas entradas

A transcrição pode já existir. Então a conversa entra por um de dois caminhos e ambos convergem no mesmo campo (`conversa.transcricao`) antes da análise:

```
Entrada A: áudio (wav/mp3/m4a) ─► normaliza p/ 16 kHz mono ─► Whisper.net (offline) ─┐
                                                                                    ├─► conversa.transcricao ─► análise IA ─► insights pendentes
Entrada B: texto (colado ou .txt) ──────────────────────────────────────────────────┘                                        + to-dos + resumo
```

A regra do worker é uma só: **só transcreve se `transcricao` estiver vazia e houver `caminho_do_audio`**. Daí saem três coisas de graça:

- a entrada por texto não precisa de código próprio no pipeline, só pula a etapa;
- se a análise falhar, reprocessar não roda o Whisper de novo;
- a demo tem um plano B: se o Whisper estiver lento na hora, cola-se o texto.

## Avisos do ambiente (verificados nesta máquina)

1. **Não há .NET SDK instalado**, só runtimes (ASP.NET Core 8.0.21). Primeiro passo: `winget install Microsoft.DotNet.SDK.8`.
2. **Whisper vai rodar só em CPU.** A máquina é um i5-1334U (10 núcleos, linha U de baixo consumo), 16 GB de RAM, sem GPU dedicada. Minha estimativa, ainda não medida: `base` roda bem mais rápido que tempo real, `small` fica perto do tempo real, `medium` e `large` são inviáveis para demo ao vivo. O spike do plano 02 mede isso antes de qualquer outra coisa.
3. **Não há ffmpeg.** A conversão de áudio fica com o NAudio, que resolve dentro do processo no Windows.
4. O repositório não tem commits nem `.gitignore`. Chaves e connection string vão em `dotnet user-secrets`, nunca no repo.

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

## Lacunas do `contexto/` e como foram resolvidas

| Lacuna | Resolução |
|---|---|
| Transcrição pode já existir | `conversa.origem` (`Audio`/`Texto`) e `caminho_do_audio` passa a ser opcional |
| "Datas importantes" e "Próximos eventos" não têm onde ficar | `informacao_insight.data_referencia` + `recorrente_anual` (aniversários) |
| "Pautas sugeridas (IA)" não têm onde ficar | `conversa.pautas_sugeridas text[]`; a Home mostra as da última conversa de cada cliente |
| Resumo são "3 bullets" | `conversa.resumo text[]` |
| Sentimento sem valores definidos | Enum fixo: `Satisfeito`, `Alinhado`, `Neutro`, `NecessitaAtencao` |
| Datas relativas na fala ("mês que vem") | A data da reunião vai no prompt; na entrada por texto o assessor informa a data |
| Whisper não separa quem fala | O prompt pede fatos só sobre o cliente. Texto colado do Teams/Meet costuma vir com os nomes, o que ajuda |
| O mesmo fato reaparece a cada reunião | Os insights já aprovados do cliente vão no prompt, pedindo só o que é novo ou mudou |
| Limite de upload do Kestrel é 30 MB | Subir para 200 MB na página de upload |
| CRUD de clientes não cabe nas "duas telas" | Página simples `/Clientes` (lista + formulário) |

## Contratos compartilhados

Definir estes tipos no início do plano 01, junto com implementações falsas. Assim quem faz tela não espera Whisper nem OpenAI.

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
    Sentimento Sentimento,
    List<InsightExtraido> Insights,
    List<AcaoExtraida> Acoes,
    List<string> PautasSugeridas);

public record InsightExtraido(
    CategoriaInsight Categoria,
    string Conteudo,
    string TrechoDeOrigem,
    DateOnly? DataReferencia,
    bool RecorrenteAnual);

public record AcaoExtraida(string Descricao, DateOnly? Data);
```

Enums (gravados como texto no banco, com o nome do enum):

- `StatusConversa`: `NaFila`, `Transcrevendo`, `Analisando`, `Concluida`, `Erro`
- `OrigemConversa`: `Audio`, `Texto`
- `Sentimento`: `Satisfeito`, `Alinhado`, `Neutro`, `NecessitaAtencao`
- `CategoriaInsight`: `Familia`, `Hobby`, `Objetivo`, `Evento`, `Profissional`, `Outro`
- `StatusRevisao`: `Pendente`, `Aprovado`, `Descartado`

## Ordem de trabalho

**Sozinho:** setup → 01 → 02 só com texto → 03 → 04 (tela do cliente) → 02 com áudio → 04 (painel e polimento). O spike de 15 minutos do Whisper acontece no setup, para o problema de hardware aparecer cedo.

**Em time**, depois de 01 pronto (schema + contratos + fakes):

| Trilha | Planos | Depende de |
|---|---|---|
| A — Pipeline e Whisper | 02 | contratos |
| B — IA | 03 | contratos |
| C — Telas | 04 | contratos + fakes + seed |

## Linha de corte

| Prioridade | Itens |
|---|---|
| **Precisa ter** | Cliente, entrada por texto, análise, revisão de insights (aprovar/descartar), perfil do cliente, entrada por áudio com Whisper |
| **Deveria ter** | Painel com to-dos e datas, edição de insight na revisão, linha do tempo, dedupe via fatos conhecidos, MP3/M4A |
| **Se sobrar tempo** | Pautas na Home, contagem regressiva, drag-and-drop, botão de reprocessar, upload de `.txt` |

Se às 5h o áudio ainda não estiver estável, a demo segue com texto e um áudio curto já processado no seed.

## Ponto para o pitch: privacidade

A transcrição é offline, mas o texto vai para a OpenAI. Para dados de cliente de corretora isso vai ser perguntado. Resposta para a banca: a demo usa dados fictícios, e a análise está atrás de `IConversationAnalysisService`, então trocar por um modelo local ou por um endpoint corporativo é troca de uma classe. Mascarar CPF e número de conta antes do envio fica como próximo passo, fora do MVP.
