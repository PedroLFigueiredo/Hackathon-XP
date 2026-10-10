# 04. Telas

**Objetivo:** as duas telas do `contexto/03`, com a entrada dupla (áudio ou texto) e a revisão de insights.

**Estimativa:** 2h30 (perfil do cliente: 1h30; painel: 35 min; polimento: 25 min).

**Depende de:** [01](01-fundacao-e-dados.md). Com os fakes e o seed, não precisa esperar os planos 02 e 03.

Bootstrap do template padrão, sem framework de front. Ações são formulários com handlers do Razor Pages (`OnPost...Async`) e redirect; JavaScript só para o polling e o drag-and-drop.

## Tela 2 — Perfil do cliente (`/Clientes/Detalhe/{id}`) — fazer primeiro

É onde o fluxo principal da demo acontece.

### Nova conversa

Um cartão com duas abas e um campo de data em comum (padrão hoje):

| Aba | Conteúdo | Handler |
|---|---|---|
| Áudio | Área de arrastar e soltar + seletor de arquivo (`.wav`, `.mp3`, `.m4a`) | `OnPostAudioAsync` |
| Texto | `textarea` para colar a transcrição | `OnPostTextoAsync` |

- [ ] Cartão com as duas abas e validação básica (arquivo ou texto obrigatório)
- [ ] Depois do envio, o cartão mostra a conversa em andamento com o status legível: "Na fila", "Transcrevendo áudio…", "Analisando com IA…"
- [ ] Polling: enquanto houver conversa em status não terminal, um `setInterval` de 2 s consulta `/api/conversas/{id}/status` e recarrega a página quando chega em `Concluida` ou `Erro`
- [ ] Em `Erro`, mostrar a `mensagem_de_erro`

### Seção 1 — Resumo da última reunião

- [ ] Selo de sentimento com cor (verde `Satisfeito`/`Alinhado`, cinza `Neutro`, âmbar `NecessitaAtencao`)
- [ ] Os 3 bullets do `resumo` da conversa `Concluida` mais recente

### Seção 2 — CRM humanizado

Mostra **só insights `Aprovado`**.

- [ ] Agrupados por categoria: Família, Estilo de vida e hobbies, Objetivos, Profissional, Outros
- [ ] Próximos eventos: insights com `data_referencia` futura, ordenados, com "faltam N dias". Para `recorrente_anual`, calcular a próxima ocorrência.

### Seção 3 — Revisão, histórico e to-dos

- [ ] **Revisão de insights:** lista dos `Pendente`, cada um com categoria, conteúdo editável, a citação (`trecho_de_origem`) em destaque e os botões Aprovar e Descartar. Handlers `OnPostAprovarAsync(id, conteudo)` e `OnPostDescartarAsync(id)`. Um "Aprovar todos" acelera a demo.
- [ ] **Linha do tempo:** conversas do cliente, da mais recente para a mais antiga, com data, origem (áudio ou texto), status e a transcrição num `<details>`
- [ ] **To-do local:** to-dos do cliente com checkbox (`OnPostAlternarTodoAsync(id)`)

## Tela 1 — Painel do assessor (`/`)

- [ ] **Widget 1 — Prioridades e alertas**
  - Datas importantes: insights `Aprovado` de toda a carteira com data nos próximos 30 dias, com nome do cliente e link para o perfil
  - Pautas sugeridas: `pautas_sugeridas` da última conversa `Concluida` de cada cliente
- [ ] **Widget 2 — Tarefas pendentes:** todos os `acao_todo` não concluídos, com cliente, data (se houver) e checkbox
- [ ] Atalho para `/Clientes` e contador de insights aguardando revisão por cliente

## Polimento da demo

- [ ] Seed fictício conferido (nenhum dado real de cliente)
- [ ] Estados vazios com texto útil ("Nenhuma conversa ainda. Envie um áudio ou cole uma transcrição.")
- [ ] Um áudio curto de teste (1–2 min) e as transcrições de exemplo do [03](03-analise-ia.md) à mão
- [ ] Ensaio do roteiro: abrir o painel → entrar no cliente → colar texto → revisar e aprovar → ver o perfil preenchido → enviar o áudio → voltar ao painel com to-dos e datas novas

## Critérios de aceite

- Do envio à aprovação dos insights sem recarregar a página manualmente
- Insight descartado não aparece no perfil; insight editado aparece com o texto corrigido
- Marcar um to-do no painel reflete no perfil do cliente e vice-versa
- O aniversário do seed aparece no painel com o cliente certo
- As duas telas funcionam com o banco só com o seed e com cliente sem nenhuma conversa

## Riscos

| Risco | Mitigação |
|---|---|
| Tempo de tela estoura | Seguir a linha de corte do [00](00-visao-geral.md): painel simples antes de enfeitar o perfil |
| Polling esquecido ligado | Só inicia se houver conversa em andamento e para no status terminal |
| Formulário de upload sem `enctype="multipart/form-data"` | O arquivo chega nulo no handler; conferir primeiro quando "o upload não funciona" |
