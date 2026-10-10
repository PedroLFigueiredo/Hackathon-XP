# Execução — Trilha Back

Para o Claude que roda em `C:\Users\figue\Hackathon-XP`, na branch `back`. Começa depois da bifurcação descrita no [EXECUCAO.md](EXECUCAO.md); as regras de execução e as regras do paralelo de lá valem aqui.

**Missão:** trocar os dois fakes pelos serviços reais. O pipeline, a fila, o mapeamento para as tabelas e o `ConversaService` já existem e não mudam de assinatura.

## Estado

- **Etapa atual:** aguardando a bifurcação
- **Observações:** —

## Limites

- Edita: `Services/`, `Program.cs`, `.csproj`, `db/`
- Não edita: `Pages/`, `wwwroot/`, `schema.sql`
- Roda em `http://localhost:5000`
- Testa pelo formulário cru que a base deixou no detalhe do cliente, ou por `curl`. A aparência da tela é da outra trilha.
- Não altera `appsettings.Development.json`: liga os serviços reais pela linha de comando, por exemplo `dotnet run --urls http://localhost:5000 --Fakes:Analise=false --Pipeline:RecuperarNoStartup=false`

---

## Etapa B1 — Análise real com a OpenAI (1h)

Plano: [03](03-analise-ia.md).

- [ ] Pacote `OpenAI`
- [ ] Schema JSON estrito (constante ao lado do serviço) com `perfil_pessoal`, `eventos`, `financeiro`, `acoes` e `pautas_sugeridas`, e o prompt de sistema
- [ ] `OpenAiConversationAnalysisService`: `gpt-4o-mini`, temperatura 0,2, `json_schema` com `strict: true`, chave em `OPENAI_API_KEY`
- [ ] Recusa ou resposta truncada viram exceção com mensagem legível
- [ ] `FatosJaConhecidos` montado no `ConversaProcessor` a partir dos itens `aprovado` do cliente nas três tabelas de informação
- [ ] Registro no DI condicionado a `Fakes:Analise`
- [ ] Três transcrições fictícias em `db/transcricoes-exemplo/`: uma rica em detalhes pessoais e financeiros, uma só de negócios, uma curta

**Pronto quando:** com `--Fakes:Analise=false`:
- a transcrição rica gera ao menos um item em `perfil_pessoal` com tag `familia`, um evento com data, um item financeiro e uma ação;
- todo item tem `trecho_origem` que aparece no texto;
- nenhum item financeiro traz recomendação de investimento ou classificação de perfil de risco;
- a transcrição só de negócios não gera fatos pessoais inventados;
- uma segunda conversa do mesmo cliente, com os itens da primeira já aprovados, não recria os mesmos fatos;
- com a chave inválida a conversa vai para `erro` com mensagem compreensível e nada é gravado nas tabelas de informação.

## Etapa B2 — Transcrição real com Whisper (1h)

Plano: [02](02-ingestao-audio-e-texto.md), Parte 2. Usa o modelo escolhido na etapa 2 da base.

- [ ] Pacotes `Whisper.net`, `Whisper.net.Runtime`, `NAudio`
- [ ] `WhisperFactory` singleton, criado de `Whisper:ModelPath`
- [ ] Checagem no startup, só quando `Fakes:Transcricao=false`: sem o arquivo do modelo, falha com mensagem dizendo onde ele deveria estar e como baixar. Nada de download automático.
- [ ] `AudioNormalizer`: `MediaFoundationReader` → mono → 16 kHz → WAV 16 bits
- [ ] `WhisperTranscriptionService` com idioma `pt`, normalizando para um temporário e apagando depois
- [ ] Registro no DI condicionado a `Fakes:Transcricao`

**Pronto quando:** com `--Fakes:Transcricao=false --Fakes:Analise=false`:
- o áudio de teste da base passa por `transcrevendo` → `analisando` → `concluido` com transcrição legível em português;
- um MP3 e um WAV 44,1 kHz estéreo também funcionam;
- um arquivo corrompido vira `erro` sem derrubar a aplicação;
- com a rede desligada a transcrição conclui (a análise falha e a conversa vai para `erro`).

## Entrega

- [ ] Commits `etapa B1` e `etapa B2` na branch `back`
- [ ] Bloco **Estado** atualizado com: tempo real de transcrição por minuto de áudio, e qualquer limitação encontrada
- [ ] Avisar a pessoa que a trilha terminou e está pronta para a etapa 6 do [EXECUCAO.md](EXECUCAO.md)

Se sobrar tempo antes do front terminar: checagem de que o `trecho_origem` de cada item existe de fato na transcrição, descartando o que não tiver.
