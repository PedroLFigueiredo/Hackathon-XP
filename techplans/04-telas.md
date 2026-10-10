# 04. Telas

**Objetivo:** as duas telas do `contexto/03`, com a entrada dupla (áudio ou texto), a revisão e a seção financeira que o `schema.sql` trouxe.

**Estimativa:** 2h40 (perfil do cliente: 1h40; painel: 35 min; polimento: 25 min).

**Depende de:** [01](01-fundacao-e-dados.md). Com os fakes e o seed, não precisa esperar os planos 02 e 03.

Bootstrap do template padrão, sem framework de front. Ações são formulários com handlers do Razor Pages (`OnPost...Async`) e redirect; JavaScript só para o polling e o drag-and-drop.

## Tela 2 — Perfil do cliente (`/Clientes/Detalhe/{id}`) — fazer primeiro

É onde o fluxo principal da demo acontece.

### Nova conversa

Um cartão com duas abas e um campo de data em comum (padrão hoje):

| Aba | Conteúdo | Handler | Chama |
|---|---|---|---|
| Áudio | Área de arrastar e soltar + seletor de arquivo (`.wav`, `.mp3`, `.m4a`) | `OnPostAudioAsync` | `ConversaService.CriarDeAudioAsync` |
| Texto | `textarea` para colar a transcrição | `OnPostTextoAsync` | `ConversaService.CriarDeTextoAsync` |

- [ ] Cartão com as duas abas e validação básica (arquivo ou texto obrigatório)
- [ ] Depois do envio, o cartão mostra a conversa em andamento com o status legível: `aguardando` → "Na fila", `transcrevendo` → "Transcrevendo áudio…", `analisando` → "Analisando com IA…"
- [ ] Polling: enquanto houver conversa em status não terminal, um `setInterval` de 2 s consulta `/api/conversas/{id}/status` e recarrega a página quando chega em `concluido` ou `erro`
- [ ] Em `erro`, mostrar a `mensagem_erro`

### Seção 1 — Resumo da última reunião

- [ ] Selo de tom com cor (verde `satisfeito`/`alinhado`, cinza `neutro`, âmbar `atencao`), de `conversas.tom`
- [ ] Os bullets de `conversas.resumo` (um por linha) da conversa `concluido` mais recente

### Seção 2 — CRM humanizado

Mostra **só itens `aprovado`**.

- [ ] `perfil_pessoal` agrupado pela primeira tag: Família, Hobbies, Estilo de vida, Profissional, Objetivos, Outros. Primeira tag desconhecida cai em Outros. As demais tags aparecem como etiquetas.
- [ ] Próximos eventos: `informacao_eventos` com data futura, ordenados, com "faltam N dias". Para a tag `anual`, calcular a próxima ocorrência a partir de `data_evento`.

### Seção 3 — Financeiro (nova)

- [ ] `informacao_financeiro` aprovada, agrupada pela primeira tag: Objetivos, Patrimônio, Renda, Liquidez, Preocupações, Outros
- [ ] Texto fixo discreto: "Registro do que o cliente disse. Não é recomendação."

### Seção 4 — Revisão, histórico e to-dos

- [ ] **Revisão:** itens `pendente` do cliente nas três tabelas, em três blocos (Pessoal, Eventos, Financeiro). Cada item tem conteúdo editável, a citação (`trecho_origem`) em destaque, as tags, a data quando for evento, e os botões Aprovar e Descartar.
  - Handlers `OnPostAprovarAsync(tipo, id, conteudo)` e `OnPostDescartarAsync(tipo, id)`, com `tipo` em `pessoal`, `evento`, `financeiro`. A interface `IRevisavel` do plano 01 deixa o corpo igual para os três.
  - "Aprovar todos" acelera a demo.
- [ ] **Linha do tempo:** conversas do cliente, da mais recente para a mais antiga, com data, origem (áudio se `caminho_audio` existe, senão texto), status e a transcrição num `<details>`
- [ ] **To-do local:** `acoes` do cliente sem a tag `pauta`, com checkbox (`OnPostAlternarAcaoAsync(id)` alterna entre `pendente` e `concluido`)

## Tela 1 — Painel do assessor (`/`)

- [ ] **Widget 1 — Prioridades e alertas**
  - Datas importantes: `informacao_eventos` aprovados de toda a carteira com data nos próximos 30 dias (próxima ocorrência para a tag `anual`), com nome do cliente e link para o perfil
  - Pautas sugeridas: `acoes` pendentes com a tag `pauta`, com o cliente. Marcar como concluída tira da lista.
- [ ] **Widget 2 — Tarefas pendentes:** `acoes` pendentes sem a tag `pauta`, com cliente, data (se houver) e checkbox
- [ ] Carteira: lista de clientes com o selo de `clientes.tom` e o número de itens aguardando revisão; atalho para `/Clientes`

## Polimento da demo

- [ ] Seed fictício conferido (nenhum dado real de cliente)
- [ ] Estados vazios com texto útil ("Nenhuma conversa ainda. Envie um áudio ou cole uma transcrição.")
- [ ] Um áudio curto de teste (1–2 min) e as transcrições de exemplo do [03](03-analise-ia.md) à mão
- [ ] Ensaio do roteiro: abrir o painel → entrar no cliente → colar texto → revisar e aprovar → ver o perfil preenchido → enviar o áudio → voltar ao painel com to-dos e datas novas

## Critérios de aceite

- Do envio à aprovação sem recarregar a página manualmente
- Item descartado não aparece no perfil; item editado aparece com o texto corrigido
- A revisão funciona para as três tabelas
- Marcar uma ação no painel reflete no perfil do cliente e vice-versa
- O aniversário do seed aparece no painel com o cliente certo
- As duas telas funcionam com o banco só com o seed e com cliente sem nenhuma conversa

## Riscos

| Risco | Mitigação |
|---|---|
| Tempo de tela estoura | Seguir a linha de corte do [00](00-visao-geral.md): painel simples antes de enfeitar o perfil |
| Consulta por tag em `text[]` | No EF com Npgsql, `Tags.Contains("pauta")` vira `'pauta' = ANY(tags)`. Para o MVP também serve filtrar em memória. |
| Polling esquecido ligado | Só inicia se houver conversa em andamento e para no status terminal |
| Formulário de upload sem `enctype="multipart/form-data"` | O arquivo chega nulo no handler; conferir primeiro quando "o upload não funciona" |
