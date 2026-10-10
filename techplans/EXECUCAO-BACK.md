# Execução — Trilha Back

Para o Claude que roda em `C:\Users\figue\Hackathon-XP`, na branch `back`. Começa depois da bifurcação descrita no [EXECUCAO.md](EXECUCAO.md); as regras de execução e as regras do paralelo de lá valem aqui.

**Missão:** trocar os dois fakes pelos serviços reais. O pipeline, a fila, o mapeamento e o `ConversaService` já existem e não mudam de assinatura.

## Estado

- **Etapa atual:** aguardando a bifurcação
- **Observações:** —

## Limites

- Edita: `Services/`, `Program.cs`, `.csproj`, `db/`
- Não edita: `Pages/`, `wwwroot/`
- Roda em `http://localhost:5000`
- Testa pelo formulário cru que a base deixou no detalhe do cliente, ou por `curl`. A aparência da tela é da outra trilha.
- Não altera `appsettings.Development.json`: liga os serviços reais pela linha de comando, por exemplo `dotnet run --urls http://localhost:5000 --Fakes:Analise=false --Pipeline:RecuperarNoStartup=false`

---

## Etapa B1 — Análise real com a OpenAI (1h)

Plano: [03](03-analise-ia.md).

- [ ] Pacote `OpenAI`
- [ ] Schema JSON estrito (constante ao lado do serviço) e prompt de sistema
- [ ] `OpenAiConversationAnalysisService`: `gpt-4o-mini`, temperatura 0,2, `json_schema` com `strict: true`
- [ ] Recusa ou resposta truncada viram exceção com mensagem legível
- [ ] `FatosJaConhecidos` montado no `ConversaProcessor` a partir dos insights `Aprovado` do cliente
- [ ] Reprocessamento apaga antes os insights `Pendente` e os to-dos não concluídos daquela conversa
- [ ] Registro no DI condicionado a `Fakes:Analise`
- [ ] Três transcrições fictícias em `db/transcricoes-exemplo/`: uma rica em detalhes pessoais, uma só de negócios, uma curta

**Pronto quando:** com `--Fakes:Analise=false`:
- a transcrição rica gera ao menos um insight de `Familia`, um de `Hobby` ou `Objetivo` e uma ação;
- todo insight tem `trecho_de_origem` que aparece no texto;
- a transcrição só de negócios não gera fatos pessoais inventados;
- analisar de novo, com os insights já aprovados, não recria os mesmos fatos;
- com a chave inválida a conversa vai para `Erro` com mensagem compreensível.

## Etapa B2 — Transcrição real com Whisper (1h)

Plano: [02](02-ingestao-audio-e-texto.md), Parte 2. Usa o modelo escolhido na etapa 2 da base.

- [ ] Pacotes `Whisper.net`, `Whisper.net.Runtime`, `NAudio`
- [ ] `WhisperFactory` singleton, criado de `Whisper:ModelPath`
- [ ] Checagem no startup, só quando `Fakes:Transcricao=false`: sem o arquivo do modelo, falha com mensagem dizendo onde ele deveria estar e como baixar. Nada de download automático.
- [ ] `AudioNormalizer`: `MediaFoundationReader` → mono → 16 kHz → WAV 16 bits
- [ ] `WhisperTranscriptionService` com idioma `pt`, normalizando para um temporário e apagando depois
- [ ] Registro no DI condicionado a `Fakes:Transcricao`

**Pronto quando:** com `--Fakes:Transcricao=false --Fakes:Analise=false`:
- o áudio de teste da base passa por `Transcrevendo` → `Analisando` → `Concluida` com transcrição legível em português;
- um MP3 e um WAV 44,1 kHz estéreo também funcionam;
- um arquivo corrompido vira `Erro` sem derrubar a aplicação;
- com a rede desligada a transcrição conclui (a análise falha e a conversa vai para `Erro`).

## Entrega

- [ ] Commits `etapa B1` e `etapa B2` na branch `back`
- [ ] Bloco **Estado** atualizado com: tempo real de transcrição por minuto de áudio, e qualquer limitação encontrada
- [ ] Avisar a pessoa que a trilha terminou e está pronta para a etapa 6 do [EXECUCAO.md](EXECUCAO.md)

Se sobrar tempo antes do front terminar: botão de reprocessar exposto como método no `ConversaService` (assinatura nova, então combinar com a pessoa antes) e checagem de que o `trecho_de_origem` existe na transcrição.
