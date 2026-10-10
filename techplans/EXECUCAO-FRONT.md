# Execução — Trilha Front

Para o Claude que roda em `C:\Users\figue\Hackathon-XP-front`, na branch `front`. Começa depois da bifurcação descrita no [EXECUCAO.md](EXECUCAO.md); as regras de execução e as regras do paralelo de lá valem aqui.

**Missão:** construir as duas telas do [04](04-telas.md) em cima do pipeline que já funciona com fakes. Nada aqui depende de Whisper ou OpenAI.

## Estado

- **Etapa atual:** aguardando a bifurcação
- **Observações:** —

## Limites

- Edita: `Pages/`, `wwwroot/`
- Não edita: `Services/`, `Program.cs`, `.csproj`, `db/`, `Models/`, `Data/`, `schema.sql`
- Antes de começar, conferir que o `.env` foi copiado para a raiz desta pasta; sem ele a aplicação não conecta no banco
- Roda sempre com os fakes ligados, em outra porta e sem recuperar conversas do outro:
  `dotnet run --urls http://localhost:5080 --Pipeline:RecuperarNoStartup=false`
- Lê dados direto pelo `AppDbContext` nos PageModels. Para criar conversa usa só `ConversaService.CriarDeTextoAsync` e `CriarDeAudioAsync`; para acompanhar, só `GET /api/conversas/{id}/status`.
- Faltou um dado ou um método? Para e avisa a pessoa. Não cria serviço nem coluna.

O que os fakes entregam: cada envio leva cerca de 3 s em `transcrevendo` (só áudio) e 3 s em `analisando`, e sempre gera os mesmos itens nas três tabelas de informação, mais uma tarefa e uma pauta. É o suficiente para testar o polling e a revisão. Os estados que os fakes não produzem (`erro`, cliente sem conversa) estão no seed.

Valores que a tela precisa conhecer (todos em minúsculas no banco):

- `conversas.status`: `aguardando`, `transcrevendo`, `analisando`, `concluido`, `erro`
- `tom`: `satisfeito`, `alinhado`, `neutro`, `atencao`
- `status_revisao`: `pendente`, `aprovado`, `descartado`
- `acoes.status_conclusao`: `pendente`, `concluido`
- Tags reservadas: `pauta` em `acoes`, `anual` em `informacao_eventos`; a primeira tag de cada item define o grupo na tela

---

## Etapa F1 — Tela do cliente (1h40)

Plano: [04](04-telas.md), Tela 2. Substitui o formulário cru da base.

- [ ] Cartão "Nova conversa" com abas Áudio e Texto e campo de data em comum
  - Áudio: arrastar e soltar + seletor (`.wav`, `.mp3`, `.m4a`), `enctype="multipart/form-data"`, `[RequestSizeLimit(200_000_000)]` e `[RequestFormLimits(MultipartBodyLengthLimit = 200_000_000)]` no PageModel
  - Texto: `textarea` com mínimo de ~50 caracteres
- [ ] Conversa em andamento com status legível ("Na fila", "Transcrevendo áudio…", "Analisando com IA…") e polling de 2 s que recarrega ao chegar em `concluido` ou `erro`
- [ ] Em `erro`, mostrar a `mensagem_erro`
- [ ] Seção 1: selo de tom com cor e os bullets do resumo da última conversa concluída
- [ ] Seção 4, revisão: itens `pendente` das três tabelas em três blocos (Pessoal, Eventos, Financeiro), com conteúdo editável, citação em destaque, tags, Aprovar, Descartar e "Aprovar todos"
- [ ] Seção 2, CRM humanizado: `perfil_pessoal` aprovado agrupado pela primeira tag, e próximos eventos com "faltam N dias" (próxima ocorrência quando a tag é `anual`)
- [ ] Seção 3, financeiro: `informacao_financeiro` aprovada agrupada pela primeira tag, com o aviso "Registro do que o cliente disse. Não é recomendação."
- [ ] Linha do tempo: conversas com data, origem (áudio ou texto), status e transcrição num `<details>`
- [ ] To-do local: `acoes` do cliente sem a tag `pauta`, com checkbox

**Pronto quando:**
- colar um texto leva, sem recarregar à mão, até a lista de revisão;
- enviar um arquivo de áudio mostra "Transcrevendo áudio…" antes de "Analisando com IA…";
- aprovar, descartar e editar funcionam para item pessoal, evento e financeiro;
- o cliente do seed com conversa em `erro` mostra a mensagem, e o cliente sem conversa mostra um estado vazio útil.

## Etapa F2 — Painel do assessor (35 min)

Plano: [04](04-telas.md), Tela 1.

- [ ] Widget 1: datas importantes dos próximos 30 dias (`informacao_eventos` aprovados de toda a carteira, com cliente e link) e pautas sugeridas (`acoes` pendentes com a tag `pauta`)
- [ ] Widget 2: `acoes` pendentes sem a tag `pauta`, de toda a carteira, com cliente, data e checkbox
- [ ] Carteira: clientes com o selo de `clientes.tom` e o número de itens aguardando revisão; atalho para `/Clientes`
- [ ] Estados vazios com texto útil nas duas telas

**Pronto quando:**
- o aniversário do seed aparece com o cliente certo;
- marcar uma ação no painel reflete no perfil do cliente e vice-versa;
- o painel abre sem erro com o banco só com o seed.

## Entrega

- [ ] Commits `etapa F1` e `etapa F2` na branch `front`
- [ ] Bloco **Estado** atualizado, com qualquer coisa que tenha ficado de fora
- [ ] Avisar a pessoa que a trilha terminou e está pronta para a etapa 6 do [EXECUCAO.md](EXECUCAO.md)

Se sobrar tempo antes do back terminar: acabamento visual das duas telas e revisão dos textos em português. Continua valendo o limite de pastas.
