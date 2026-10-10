# Execução — Trilha Front

Para o Claude que roda em `C:\Users\figue\Hackathon-XP-front`, na branch `front`. Começa depois da bifurcação descrita no [EXECUCAO.md](EXECUCAO.md); as regras de execução e as regras do paralelo de lá valem aqui.

**Missão:** construir o perfil do cliente, o painel do assessor, a lista de clientes, a sincronização por CSV e uma tela separada para preparar a demonstração, em cima do pipeline com fakes. Nada aqui depende de Whisper ou OpenAI.

## Decisões atualizadas do MVP

- Todas as informações extraídas (pessoais, eventos e financeiras) entram automaticamente no perfil. Não há aprovação, lista de revisão nem contador de revisão pendente. O assessor pode editar ou excluir informações depois.
- O perfil é a memória do relacionamento de um cliente que já existe no CRM. Dados cadastrais importados ficam separados das informações extraídas das conversas.
- A sincronização com o CRM é feita por um CSV padrão, sem integração direta com um fornecedor neste MVP.
- A preparação dos seis clientes fictícios fica exclusivamente em `/Demo`, acessada diretamente pela URL e fora da navegação principal. Não colocar botão de carga de demonstração na lista de clientes nem no perfil.
- Estas decisões substituem os requisitos de aprovação e revisão dos planos [00](00-visao-geral.md), [03](03-analise-ia.md) e [04](04-telas.md) e dos roteiros [EXECUCAO.md](EXECUCAO.md) e [EXECUCAO-BACK.md](EXECUCAO-BACK.md). Esses documentos ainda precisam ser alinhados pela trilha responsável.

## Estado

- **Etapa atual:** aguardando a bifurcação
- **Observações:** planejamento atualizado; implementação ainda não iniciada. Dependências de cadastro CRM, importação/exportação e carga de demonstração precisam estar disponíveis na base.

## Limites

- Edita: `Pages/`, `wwwroot/`
- Não edita: `Services/`, `Program.cs`, `.csproj`, `db/`, `Models/`, `Data/`, `schema.sql`
- Antes de começar, conferir que o `.env` foi copiado para a raiz desta pasta; sem ele a aplicação não conecta no banco
- Roda sempre com os fakes ligados, em outra porta e sem recuperar conversas do outro:
  `dotnet run --urls http://localhost:5080 --Pipeline:RecuperarNoStartup=false`
- Lê dados direto pelo `AppDbContext` nos PageModels. Para criar conversa usa só `ConversaService.CriarDeTextoAsync` e `CriarDeAudioAsync`; para acompanhar, só `GET /api/conversas/{id}/status`.
- Faltou um dado ou um método? Para e avisa a pessoa. Não cria serviço nem coluna.

### Dependências da base/back

- Identificador externo `crm_id` no cadastro, com unicidade para evitar duplicação. **O `schema.sql` atual não tem essa coluna em `clientes`**; ela precisa ser aprovada pelo time e incluída antes das etapas F3 e F4.
- Operação de sincronização que valida o CSV e cria/atualiza os clientes no Supabase pelo `crm_id`, preservando conversas, informações e tarefas. Clientes ausentes do arquivo não são excluídos.
- Operações de exportação cadastral em CSV e de perfil em JSON.
- Informações disponíveis automaticamente após a análise, sem depender de `status_revisao = aprovado`; suporte a edição e exclusão posterior. O `schema.sql` mantém `status_revisao` nas três tabelas de informação, então a compatibilidade (por exemplo, gravar já como `aprovado`) é responsabilidade da base/back, que também define qual filtro a tela usa para "informação ativa".
- Carga dos seis clientes fictícios usando o mesmo fluxo de sincronização, sem duplicar cadastros em execuções repetidas.
- Os nomes dos métodos e contratos dessas operações devem ser definidos com a base/back antes da implementação. A trilha front não altera schema nem cria serviços para contornar dependências.

O que os fakes entregam: cada envio leva cerca de 3 s em `transcrevendo` (só áudio) e 3 s em `analisando`, e sempre gera os mesmos itens nas três tabelas de informação, mais uma tarefa e uma pauta. É o suficiente para testar o polling e a entrada automática das informações no perfil. Os estados que os fakes não produzem (`erro`, cliente sem conversa) estão no seed.

Valores que a tela precisa conhecer (todos em minúsculas no banco, conforme o `schema.sql`):

- `conversas.status`: `aguardando`, `transcrevendo`, `analisando`, `concluido`, `erro`
- `tom`: `satisfeito`, `alinhado`, `neutro`, `atencao`
- `acoes.status_conclusao`: `pendente`, `concluido`
- Tags reservadas: `pauta` em `acoes`, `anual` em `informacao_eventos`; a primeira tag de cada item define o grupo na tela

---

## Etapa F1 — Tela do cliente (1h30)

Plano: [04](04-telas.md), Tela 2. Substitui o formulário cru da base.

- [ ] Cabeçalho com nome, e-mail, telefone, identificador do CRM, voltar à lista e botão "Exportar perfil" (JSON com cadastro, informações, tarefas e histórico)
- [ ] Botão "Adicionar conversa" abre um cartão recolhível com abas Áudio e Texto e campo de data em comum (padrão hoje)
  - Áudio: seletor (`.wav`, `.mp3`, `.m4a`), `enctype="multipart/form-data"`, `[RequestSizeLimit(200_000_000)]` e `[RequestFormLimits(MultipartBodyLengthLimit = 200_000_000)]` no PageModel; arrastar e soltar é opcional
  - Texto: `textarea` com mínimo de ~50 caracteres
- [ ] Conversa em andamento com status legível ("Na fila", "Transcrevendo áudio…", "Analisando com IA…") e polling de 2 s que recarrega ao chegar em `concluido` ou `erro`
- [ ] Em `erro`, mostrar a `mensagem_erro`
- [ ] Seção 1: data e os bullets do resumo da última conversa concluída; selo "Tom da conversa — estimado pela IA" com cor, de `conversas.tom`
- [ ] Seção 2, memória do relacionamento: todas as informações ativas entram automaticamente; ocultar grupos vazios
  - `perfil_pessoal` agrupado pela primeira tag: Família, Hobbies, Estilo de vida, Vida profissional, Objetivos e Outros
  - Datas importantes, de `informacao_eventos`
  - Financeiro, de `informacao_financeiro`, agrupado pela primeira tag, com o aviso "Registro do que o cliente disse. Não é recomendação."
- [ ] Cada informação mostra conteúdo, tags, acesso à citação (`trecho_origem`) e ações Editar e Excluir, nas três tabelas. A data da conversa de origem aparece quando existir (só `perfil_pessoal` tem `conversa_id`). Não exibir Aprovar, Descartar nem "Aprovar todos"
- [ ] Próximos eventos ordenados por data (próxima ocorrência quando a tag é `anual`); contagem regressiva é opcional
- [ ] Ao concluir, destacar o perfil atualizado e informar quantas informações e tarefas foram adicionadas, quando esses dados estiverem disponíveis no contrato
- [ ] Linha do tempo: conversas com data, origem (áudio se há `caminho_audio`, senão texto), status e transcrição num `<details>`
- [ ] To-do local: `acoes` do cliente sem a tag `pauta`, com checkbox

**Pronto quando:**
- colar um texto atualiza automaticamente o resumo, a memória e as tarefas, sem recarregar à mão nem aprovar nada;
- enviar um arquivo de áudio mostra "Transcrevendo áudio…" antes de "Analisando com IA…";
- editar grava o texto corrigido e excluir remove a informação da memória e dos alertas, para item pessoal, evento e financeiro;
- exportar perfil gera um JSON do cliente selecionado;
- o cliente do seed com conversa em `erro` mostra a mensagem, e o cliente sem conversa mostra um estado vazio útil.

## Etapa F2 — Painel do assessor (35 min)

Plano: [04](04-telas.md), Tela 1.

- [ ] Widget 1: datas importantes dos próximos 30 dias (`informacao_eventos` ativos de toda a carteira, incluídos automaticamente, com cliente e link)
- [ ] Widget 2: `acoes` pendentes sem a tag `pauta`, de toda a carteira, com cliente, data e checkbox
- [ ] Atalho para `/Clientes`; não mostrar contador de revisão pendente
- [ ] Pautas sugeridas (`acoes` pendentes com a tag `pauta`) apenas se sobrar tempo
- [ ] Estados vazios com texto útil nas duas telas

**Pronto quando:**
- o aniversário do seed aparece com o cliente certo;
- marcar uma ação no painel reflete no perfil do cliente e vice-versa;
- o painel abre sem erro com o banco só com o seed.

## Etapa F3 — Clientes e sincronização CRM

### Lista (`/Clientes`)

- [ ] Busca por nome e lista com nome, última conversa e acesso ao perfil
- [ ] Botão "Sincronizar CRM" abre `/Clientes/Sincronizar`
- [ ] Botão "Exportar CSV" baixa os dados cadastrais no mesmo padrão de importação
- [ ] Sem filtros avançados, gráficos ou carga de demonstração nesta tela

### Sincronização (`/Clientes/Sincronizar`)

- [ ] Botão "Baixar modelo CSV", seleção do arquivo e prévia antes de gravar
- [ ] Prévia indica novos clientes, atualizações e erros por linha
- [ ] Botão "Sincronizar clientes" grava no Supabase e mostra quantidades criadas/atualizadas e erros
- [ ] `crm_id` e `nome` obrigatórios; e-mail e telefone opcionais; IDs repetidos no arquivo são reportados como erro
- [ ] Padronizar CSV em UTF-8, com vírgula como separador e cabeçalho abaixo; telefone tratado como texto
- [ ] Reimportar atualiza apenas o cadastro pelo `crm_id`, preservando o histórico; não excluir clientes ausentes do CSV

```csv
crm_id,nome,email,telefone
DEMO-001,Ana Martins,ana@example.com,11900000001
DEMO-002,Bruno Oliveira,bruno@example.com,11900000002
DEMO-003,Carla Santos,carla@example.com,11900000003
DEMO-004,Diego Ferreira,diego@example.com,11900000004
DEMO-005,Elisa Costa,elisa@example.com,11900000005
DEMO-006,Felipe Almeida,felipe@example.com,11900000006
```

**Pronto quando:** importar cria/atualiza clientes reais no Supabase, repetir o mesmo CSV não duplica cadastros, erros ficam legíveis e o CSV exportado pode ser reimportado.

## Etapa F4 — Preparação da demonstração (`/Demo`)

- [ ] Tela separada, acessada diretamente por URL e fora da navegação principal
- [ ] Prévia dos seis clientes fictícios definidos no modelo CSV
- [ ] Botão "Carregar clientes no Supabase" usa o mesmo fluxo de sincronização; abrir a tela não grava dados
- [ ] Mostrar resultado com quantidades criadas/atualizadas e eventuais erros
- [ ] Botão "Abrir clientes" leva a `/Clientes` para iniciar a apresentação
- [ ] Repetir a carga mantém os mesmos seis clientes e preserva conversas, informações e tarefas existentes
- [ ] A carga inicial contém apenas cadastros, prontos para receber conversas; não cria análises nem informações fictícias automaticamente

**Pronto quando:** os seis clientes aparecem na lista após a carga, uma segunda execução não cria duplicatas e a preparação da demo não aparece nas telas normais.

## Corte de escopo

Priorizar perfil com informações automáticas, entrada de conversa, lista, sincronização CSV e tela `/Demo`. Manter o painel enxuto com tarefas e datas. Drag-and-drop, pautas na Home e contagem regressiva ficam para depois. Sem autenticação, integração direta com CRM, calendário, gravação pelo navegador ou player sincronizado neste MVP.

As estimativas originais de F1/F2 não incluem F3/F4; reavaliar o tempo com as dependências da base/back antes de iniciar a implementação.

## Entrega

- [ ] Commits por etapa F1, F2, F3 e F4 na branch `front`
- [ ] Validar o roteiro: abrir `/Demo` → carregar seis clientes → abrir lista → acessar perfil → colar conversa → ver informações automáticas → editar uma informação → exportar perfil
- [ ] Validar importação/exportação CSV e repetição da carga sem duplicação
- [ ] Bloco **Estado** atualizado, com qualquer coisa que tenha ficado de fora
- [ ] Avisar a pessoa que a trilha terminou e está pronta para a etapa 6 do [EXECUCAO.md](EXECUCAO.md)

Se sobrar tempo antes do back terminar: acabamento visual e revisão dos textos em português. Continua valendo o limite de pastas.
