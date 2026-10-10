# Execução — Trilha Front

Para o Claude que roda em `C:\Users\figue\Hackathon-XP-front`, na branch `front`. Começa depois da bifurcação descrita no [EXECUCAO.md](EXECUCAO.md); as regras de execução e as regras do paralelo de lá valem aqui.

**Missão:** construir as duas telas do [04](04-telas.md) em cima do pipeline que já funciona com fakes. Nada aqui depende de Whisper ou OpenAI.

## Estado

- **Etapa atual:** aguardando a bifurcação
- **Observações:** —

## Limites

- Edita: `Pages/`, `wwwroot/`
- Não edita: `Services/`, `Program.cs`, `.csproj`, `db/`, `Models/`, `Data/`
- Roda sempre com os fakes ligados, em outra porta e sem recuperar conversas do outro:
  `dotnet run --urls http://localhost:5080 --Pipeline:RecuperarNoStartup=false`
- Lê dados direto pelo `AppDbContext` nos PageModels. Para criar conversa usa só `ConversaService.CriarDeTextoAsync` e `CriarDeAudioAsync`; para acompanhar, só `GET /api/conversas/{id}/status`.
- Faltou um dado ou um método? Para e avisa a pessoa. Não cria serviço nem coluna.

O que os fakes entregam: cada envio leva cerca de 3 s em `Transcrevendo` (só áudio) e 3 s em `Analisando`, e sempre gera os mesmos insights e to-dos. É o suficiente para testar o polling e a revisão. Os estados que os fakes não produzem (`Erro`, cliente sem conversa) estão no seed.

---

## Etapa F1 — Tela do cliente (1h30)

Plano: [04](04-telas.md), Tela 2. Substitui o formulário cru da base.

- [ ] Cartão "Nova conversa" com abas Áudio e Texto e campo de data em comum
  - Áudio: arrastar e soltar + seletor (`.wav`, `.mp3`, `.m4a`), `enctype="multipart/form-data"`, `[RequestSizeLimit(200_000_000)]` e `[RequestFormLimits(MultipartBodyLengthLimit = 200_000_000)]` no PageModel
  - Texto: `textarea` com mínimo de ~50 caracteres
- [ ] Conversa em andamento com status legível ("Na fila", "Transcrevendo áudio…", "Analisando com IA…") e polling de 2 s que recarrega ao chegar em `Concluida` ou `Erro`
- [ ] Em `Erro`, mostrar a `mensagem_de_erro`
- [ ] Seção 1: selo de sentimento com cor e os 3 bullets do resumo da última conversa concluída
- [ ] Seção 3, revisão: insights `Pendente` com categoria, conteúdo editável, citação em destaque, Aprovar, Descartar e "Aprovar todos"
- [ ] Seção 2, CRM humanizado: só insights `Aprovado`, agrupados por categoria, e próximos eventos com "faltam N dias" (próxima ocorrência quando `recorrente_anual`)
- [ ] Linha do tempo: conversas com data, origem, status e transcrição num `<details>`
- [ ] To-do local com checkbox

**Pronto quando:**
- colar um texto leva, sem recarregar à mão, até a lista de revisão;
- enviar um arquivo de áudio mostra "Transcrevendo áudio…" antes de "Analisando com IA…";
- aprovar move o insight para o perfil, descartar some com ele, editar grava o texto novo;
- o cliente do seed com conversa em `Erro` mostra a mensagem, e o cliente sem conversa mostra um estado vazio útil.

## Etapa F2 — Painel do assessor (35 min)

Plano: [04](04-telas.md), Tela 1.

- [ ] Widget 1: datas importantes dos próximos 30 dias (insights `Aprovado` de toda a carteira, com cliente e link) e pautas sugeridas da última conversa concluída de cada cliente
- [ ] Widget 2: to-dos pendentes de toda a carteira com cliente, data e checkbox
- [ ] Contador de insights aguardando revisão por cliente e atalho para `/Clientes`
- [ ] Estados vazios com texto útil nas duas telas

**Pronto quando:**
- o aniversário do seed aparece com o cliente certo;
- marcar um to-do no painel reflete no perfil do cliente e vice-versa;
- o painel abre sem erro com o banco só com o seed.

## Entrega

- [ ] Commits `etapa F1` e `etapa F2` na branch `front`
- [ ] Bloco **Estado** atualizado, com qualquer coisa que tenha ficado de fora
- [ ] Avisar a pessoa que a trilha terminou e está pronta para a etapa 6 do [EXECUCAO.md](EXECUCAO.md)

Se sobrar tempo antes do back terminar: acabamento visual das duas telas e revisão dos textos em português. Continua valendo o limite de pastas.
