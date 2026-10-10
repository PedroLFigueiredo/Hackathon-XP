# 02. Ingestão: Áudio e Texto

**Objetivo:** receber uma conversa por áudio ou por texto, processar em background e deixar `conversa.transcricao` preenchida para a análise do [03](03-analise-ia.md).

**Estimativa:** 1h45 (texto e worker: 45 min; Whisper: 1h).

**Depende de:** [01](01-fundacao-e-dados.md).

## Máquina de estados

```
            origem = Audio                    origem = Texto
                 │                                  │
              NaFila ◄──────────────────────────────┘
                 │
   transcricao vazia? ── sim ──► Transcrevendo ──┐
                 │ não                           │
                 └──────────────► Analisando ◄───┘
                                      │
                               Concluida | Erro
```

`Erro` guarda o motivo em `mensagem_de_erro`. Cada etapa grava o status no banco antes de começar, para a tela poder mostrar.

## Parte 1 — Texto e worker (fazer primeiro)

- [ ] `ConversaQueue`: wrapper singleton sobre `Channel<Guid>` com `Enfileirar(Guid)` e `LerAsync(ct)`
- [ ] `ConversaProcessor` (scoped), método `ProcessarAsync(Guid conversaId, ct)`:
  1. carrega a conversa e o cliente;
  2. se `transcricao` está vazia e há `caminho_do_audio`: status `Transcrevendo`, chama `ITranscriptionService`, grava a transcrição;
  3. status `Analisando`, chama `IConversationAnalysisService`, grava o resultado (o mapeamento está no [03](03-analise-ia.md));
  4. status `Concluida`;
  5. qualquer exceção: status `Erro` com a mensagem. Nunca derruba o worker.
- [ ] `ConversaWorker : BackgroundService`: lê a fila um item por vez e cria um escopo de DI por job (`IServiceScopeFactory`), porque o `DbContext` é scoped e o worker é singleton
- [ ] Recuperação no startup: enfileirar conversas em `NaFila`, `Transcrevendo` ou `Analisando`
- [ ] Handler de texto no perfil do cliente: recebe `data` (padrão hoje) e `texto` (mínimo ~50 caracteres), cria a conversa com `origem = Texto` e `transcricao` já preenchida, enfileira
- [ ] Endpoint `GET /api/conversas/{id}/status` devolvendo `{ status, mensagemDeErro }` para o polling da tela

Com os fakes do plano 01, esta parte já fecha o ciclo completo: colar texto → `Analisando` → `Concluida`.

## Parte 2 — Áudio com Whisper offline

### Spike (15 min, no setup)

- [ ] Baixar o modelo para `models/` (única etapa com internet):
  ```
  curl -L -o models/ggml-small.bin https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-small.bin
  ```
- [ ] Console mínimo transcrevendo um áudio em português de 1 minuto. Anotar: tempo de carga do modelo, tempo de transcrição, qualidade dos nomes próprios.
- [ ] Decisão: se `small` levar mais de ~1,5× a duração do áudio, cair para `ggml-base.bin` e limitar os áudios da demo a 1–2 minutos

### Implementação

- [ ] Pacotes: `Whisper.net`, `Whisper.net.Runtime` (CPU) e `NAudio`
- [ ] `WhisperFactory` como singleton, criado de `Whisper:ModelPath`. Carregar o modelo é a parte cara; o processor é criado por transcrição.
- [ ] Checagem no startup: se o arquivo do modelo não existe, falhar com a mensagem "modelo não encontrado em X, baixe com o comando Y". Nada de download automático.
- [ ] `AudioNormalizer.ParaWav16kMono(origem, destino)` com NAudio: `MediaFoundationReader` → mono (se estéreo) → `WdlResamplingSampleProvider(16000)` → `WaveFileWriter.CreateWaveFile16`. O `MediaFoundationReader` lê WAV, MP3 e M4A no Windows sem ffmpeg.
- [ ] `WhisperTranscriptionService`: normaliza para um arquivo temporário, roda o processor com `WithLanguage("pt")`, concatena o texto dos segmentos, apaga o temporário
- [ ] Handler de áudio no perfil do cliente: valida extensão (`.wav`, `.mp3`, `.m4a`), salva em `uploads/{conversaId}{ext}`, cria a conversa com `origem = Audio`, enfileira
- [ ] Limite de upload: `[RequestSizeLimit(200_000_000)]` e `[RequestFormLimits(MultipartBodyLengthLimit = 200_000_000)]` no PageModel

## Critérios de aceite

- Colar um texto cria a conversa e ela chega a `Concluida` sem passar por `Transcrevendo`
- Enviar um MP3 ou WAV de 1 minuto passa por `Transcrevendo` → `Analisando` → `Concluida`, e a transcrição fica legível em português
- Com a rede desligada, a transcrição continua funcionando (a análise falha e a conversa vai para `Erro` com mensagem)
- Arquivo corrompido ou de tipo errado gera `Erro` na conversa, sem derrubar a aplicação
- Matar e subir a aplicação no meio de um job retoma o processamento

## Riscos

| Risco | Mitigação |
|---|---|
| Whisper lento na CPU da linha U | Medir no spike; `base` como fallback; áudios curtos na demo; caminho de texto como plano B |
| Áudio fora de 16 kHz mono | Todo áudio passa pelo `AudioNormalizer`, inclusive WAV |
| Trechos de silêncio geram texto inventado | Usar gravação limpa na demo; a revisão humana dos insights segura o resto |
| Whisper não identifica quem fala | Tratado no prompt do [03](03-analise-ia.md) |
| Dois uploads seguidos | A fila processa um por vez; o segundo fica em `NaFila` e a tela mostra isso |
