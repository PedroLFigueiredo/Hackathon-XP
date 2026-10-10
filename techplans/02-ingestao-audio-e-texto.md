# 02. Ingestão: Áudio e Texto

**Objetivo:** receber uma conversa por áudio ou por texto, processar em background e deixar `conversas.transcricao` preenchida para a análise do [03](03-analise-ia.md).

**Estimativa:** 1h45 (texto e worker: 45 min; Whisper: 1h).

**Depende de:** [01](01-fundacao-e-dados.md).

## Máquina de estados

Valores de `conversas.status`:

```
      com áudio (caminho_audio)         por texto (caminho_audio nulo)
                 │                                  │
            aguardando ◄────────────────────────────┘
                 │
   transcricao vazia? ── sim ──► transcrevendo ──┐
                 │ não                           │
                 └──────────────► analisando ◄───┘
                                      │
                               concluido | erro
```

`erro` guarda o motivo em `mensagem_erro`. Cada etapa grava o status no banco antes de começar, para a tela poder mostrar.

## Parte 1 — Texto e worker (fazer primeiro)

- [ ] `ConversaQueue`: wrapper singleton sobre `Channel<Guid>` com `Enfileirar(Guid)` e `LerAsync(ct)`
- [ ] `ConversaProcessor` (scoped), método `ProcessarAsync(Guid conversaId, ct)`:
  1. carrega a conversa e o cliente;
  2. se `transcricao` está vazia e há `caminho_audio`: status `transcrevendo`, chama `ITranscriptionService`, grava a transcrição;
  3. status `analisando`, chama `IConversationAnalysisService`;
  4. grava o resultado e o status `concluido` **num único `SaveChanges`** (o mapeamento está no [03](03-analise-ia.md)). Assim uma falha no meio não deixa dados pela metade e tentar de novo não duplica nada;
  5. qualquer exceção: status `erro` com a mensagem. Nunca derruba o worker.
- [ ] `ConversaWorker : BackgroundService`: lê a fila um item por vez e cria um escopo de DI por job (`IServiceScopeFactory`), porque o `DbContext` é scoped e o worker é singleton
- [ ] Recuperação no startup: enfileirar conversas em `aguardando`, `transcrevendo` ou `analisando`. Desligável por `Pipeline:RecuperarNoStartup`.
- [ ] `ConversaService`, o ponto de contato único entre tela e pipeline:
  ```csharp
  Task<Guid> CriarDeTextoAsync(Guid clienteId, DateOnly data, string texto, CancellationToken ct);
  Task<Guid> CriarDeAudioAsync(Guid clienteId, DateOnly data, Stream arquivo, string nomeArquivo, CancellationToken ct);
  ```
  - texto: mínimo ~50 caracteres; cria a conversa com `transcricao` preenchida e `caminho_audio` nulo; enfileira
  - áudio: valida a extensão (`.wav`, `.mp3`, `.m4a`), salva em `uploads/{conversaId}{ext}`, cria a conversa com `caminho_audio`; enfileira
  - `conversas.data` recebe a data escolhida com a hora atual, em UTC, para a ordenação por conversa mais recente funcionar
- [ ] Endpoint `GET /api/conversas/{id}/status` devolvendo `{ status, mensagemErro }` para o polling da tela

Com os fakes do plano 01, esta parte já fecha o ciclo completo: colar texto → `analisando` → `concluido`.

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
- [ ] Checagem no startup, só com `Fakes:Transcricao=false`: se o arquivo do modelo não existe, falhar com a mensagem "modelo não encontrado em X, baixe com o comando Y". Nada de download automático.
- [ ] `AudioNormalizer.ParaWav16kMono(origem, destino)` com NAudio: `MediaFoundationReader` → mono (se estéreo) → `WdlResamplingSampleProvider(16000)` → `WaveFileWriter.CreateWaveFile16`. O `MediaFoundationReader` lê WAV, MP3 e M4A no Windows sem ffmpeg.
- [ ] `WhisperTranscriptionService`: normaliza para um arquivo temporário, roda o processor com `WithLanguage("pt")`, concatena o texto dos segmentos, apaga o temporário
- [ ] Limite de upload na página (trilha do front): `[RequestSizeLimit(200_000_000)]` e `[RequestFormLimits(MultipartBodyLengthLimit = 200_000_000)]` no PageModel

## Critérios de aceite

- Colar um texto cria a conversa e ela chega a `concluido` sem passar por `transcrevendo`
- Enviar um MP3 ou WAV de 1 minuto passa por `transcrevendo` → `analisando` → `concluido`, e a transcrição fica legível em português
- Com a rede desligada, a transcrição continua funcionando (a análise falha e a conversa vai para `erro` com mensagem)
- Arquivo corrompido ou de tipo errado gera `erro` na conversa, sem derrubar a aplicação
- Matar e subir a aplicação no meio de um job retoma o processamento

## Riscos

| Risco | Mitigação |
|---|---|
| Whisper lento na CPU da linha U | Medir no spike; `base` como fallback; áudios curtos na demo; caminho de texto como plano B |
| Áudio fora de 16 kHz mono | Todo áudio passa pelo `AudioNormalizer`, inclusive WAV |
| Trechos de silêncio geram texto inventado | Usar gravação limpa na demo; a revisão humana segura o resto |
| Whisper não identifica quem fala | Tratado no prompt do [03](03-analise-ia.md) |
| Dois uploads seguidos | A fila processa um por vez; o segundo fica em `aguardando` e a tela mostra isso |
