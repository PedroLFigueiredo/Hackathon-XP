# 03. Fluxo de Telas e Painel UI/UX

Documento atualizado em 10/10/2026, conforme as páginas e os comportamentos implementados no projeto. Descreve o fluxo atual do MVP, incluindo análise por IA e consulta de insights.

## 1. Navegação e fluxo principal

O cabeçalho compartilhado oferece **Início**, **Clientes**, **Insights** e **Demonstração**. As telas são entregues por ASP.NET Core Razor Pages, com Bootstrap e JavaScript para abas, detalhes em modal e acompanhamento do processamento.

| Tela | Rota | Objetivo |
| --- | --- | --- |
| Início | `/` | Apresentar a solução e encaminhar para a carteira de clientes. |
| Clientes | `/Clientes` | Listar os clientes e abrir um perfil. |
| Perfil do cliente | `/Clientes/Detalhe/{id}` | Registrar conversas, consultar memória, insights, ações e histórico. |
| Insights | `/Insights` ou `/Insights?cliente={id}` | Selecionar um cliente e consultar os sinais identificados pela IA. |
| Demonstração | `/Demo` | Criar ou atualizar seis cadastros fictícios. |

Fluxo principal: **Início → Clientes → Abrir perfil → Adicionar conversa → Acompanhar processamento → Consultar resultados → Concluir ou reabrir ações**.

Fluxo alternativo: **Insights → Selecionar cliente → Ver detalhes de um insight → Abrir perfil completo**.

## 2. Início: apresentação da solução

A página inicial é uma apresentação do produto, e não um painel operacional da carteira.

- Apresenta a proposta de transformar conversas em memória de relacionamento.
- Oferece os botões **Abrir clientes** e **Conhecer meus clientes**, que levam à listagem.
- O link **Conheça a solução** leva à seção explicativa da própria página.
- Explica três etapas: registrar a conversa, encontrar o que importa e acompanhar os próximos passos.
- Exibe um cartão ilustrativo de contexto do cliente, identificado como exemplo; ele não representa dados consultados do banco.
- Mostra um aviso quando transcrição ou análise fictícias estão ativadas.

## 3. Clientes: carteira cadastrada

A listagem apresenta os clientes em ordem alfabética, com nome, perfil financeiro, personalidade, e-mail e telefone. Campos de perfil ainda não preenchidos aparecem como **—**.

Cada linha oferece **Abrir perfil**, levando ao detalhe daquele cliente. Não há formulário de cadastro ou edição nessa tela.

Estados da página:

- Sem banco configurado: aviso de configuração.
- Falha de consulta: mensagem para verificar conexão e schema do Supabase.
- Sem clientes: convite com link para criar os seis cadastros na Demonstração.

## 4. Perfil do cliente: centro do atendimento

### 4.1. Identificação e contexto

O topo exibe nome, e-mail e telefone, além dos campos disponíveis de perfil financeiro, personalidade estimada pela IA e tom da última conversa concluída. O link **← Clientes** retorna à carteira.

A tela informa quando serviços fictícios estão ativos. Sem chave da OpenAI e sem análise fictícia, informa que a análise automática está desligada e que a transcrição pode ser consultada no histórico.

### 4.2. Última conversa

O cartão mostra a conversa concluída mais recente na ordenação por data registrada, com data, origem (áudio ou texto), tom e resumo em tópicos quando disponíveis.

Os rótulos de tom são **Satisfeito**, **Alinhado**, **Neutro** e **Requer atenção**. Sem conversa concluída, a tela orienta o envio de áudio ou texto; sem resumo, mostra que a conversa não tem resumo.

### 4.3. Adicionar conversa

A área **Nova conversa** possui duas abas:

| Aba | Dados solicitados | Ação e validação |
| --- | --- | --- |
| Colar transcrição (aberta por padrão) | Data da conversa e texto | **Processar texto**; transcrição com pelo menos 50 caracteres após remover espaços nas extremidades. |
| Enviar áudio | Data da conversa e arquivo | **Processar áudio**; WAV, MP3 ou M4A, arquivo não vazio de até 200 MB. |

O áudio é escolhido por um campo de arquivo. Após salvar, o sistema retorna ao perfil e coloca a conversa na fila de processamento em segundo plano.

### 4.4. Insights para o assessor

Os cartões exibem sinais identificados pela IA, associados às conversas, com os rótulos:

- Apetite a risco.
- Capacidade financeira.
- Oportunidade de conversa.
- Ponto de atenção.

Ao clicar, abre-se um modal com o sinal percebido, sugestão de abordagem quando disponível, fala do cliente que fundamenta o insight e data da conversa de origem.

A seção explica que os sinais não são recomendação de investimento nem substituem o suitability. Sem sinais, apresenta um estado vazio. Se a tabela de insights não puder ser consultada, mostra um aviso sem impedir a leitura do restante do perfil.

### 4.5. Memória do relacionamento

Apresenta as informações registradas a partir das conversas:

- **Informações pessoais**, agrupadas em Família, Hobbies e estilo de vida, Vida profissional, Objetivos de vida e Outras informações.
- **Datas e eventos**, com data, distância em dias e indicação de recorrência anual quando registrada. Eventos próximos aparecem primeiro, seguidos dos sem data e dos passados.
- **Financeiro**, com categorias como Objetivo, Patrimônio, Renda, Liquidez e Preocupação.

Cada item abre um modal com seu conteúdo, palavras-chave e trecho de origem quando disponível. Informações pessoais também podem mostrar a data da conversa de origem. O detalhe financeiro identifica o conteúdo como registro do que o cliente disse, sem recomendação de investimento.

Sem informações, a seção convida o assessor a enviar uma conversa.

### 4.6. Ações e pautas

A seção separa **Tarefas do assessor** e **Pautas para a próxima conversa**. Cada grupo mostra a quantidade de itens pendentes.

- Itens pendentes aparecem antes dos concluídos, com os prazos mais próximos primeiro.
- O círculo ao lado do item permite concluir ou reabrir a ação.
- O texto abre um modal com descrição, prazo quando disponível, palavras-chave e data da conversa de origem quando disponível.
- O modal também oferece **Marcar como concluída** ou **Reabrir**.
- Após a alteração, a página retorna à seção de ações.

### 4.7. Histórico de conversas e processamento

O histórico apresenta as conversas da data registrada mais recente para a mais antiga, com data, origem, status e tom quando disponível.

| Estado | Comportamento na interface |
| --- | --- |
| Na fila | Conversa salva, aguardando processamento. |
| Transcrevendo áudio | Exibe progresso e texto parcial conforme os trechos ficam disponíveis. |
| Analisando | Indica a etapa de análise do texto. |
| Concluída | **Ver conversa** abre o resumo e a transcrição em modal. |
| Erro | Exibe a mensagem da falha e **Tentar novamente**. |

O navegador consulta `/api/conversas/{id}/status` a cada dois segundos enquanto houver conversas em processamento. Ao detectar conclusão ou erro, recarrega a página para apresentar o estado persistido e os resultados disponíveis.

Conversas ainda não concluídas mantêm a transcrição em uma área expansível na própria página. Quando um backup parcial é recuperado, o título informa **Transcrição parcial — processamento interrompido**. Conversas reconhecidas como transcrição fictícia recebem um aviso específico.

**Tentar novamente** recoloca conversas com erro na fila. Quando o áudio está em outro computador e não há transcrição recuperável, o sistema orienta tentar na máquina que recebeu o arquivo.

## 5. Insights: consulta por cliente

A página oferece uma lista de clientes em ordem alfabética, com a quantidade de insights de cada um. Inicialmente, solicita a seleção de um cliente, sem selecionar automaticamente o primeiro.

Ao selecionar, exibe:

- Nome e contagem de insights e de conversas que originaram esses insights.
- Perfil financeiro e personalidade, quando disponíveis.
- Cartões de insights das conversas mais recentes primeiro, com o mesmo modal de detalhes do perfil.
- Link **Abrir perfil completo**.

Sem insights, oferece um link para enviar uma conversa no perfil. Também trata ausência de banco, carteira vazia e falhas de consulta. Um cliente inexistente solicitado pela URL retorna página não encontrada.

## 6. Demonstração: preparação dos cadastros

A página mostra os seis clientes fictícios com nome, e-mail e telefone. O botão **Criar 6 clientes fictícios** salva os cadastros no banco; **Abrir clientes** leva à listagem.

Perfil financeiro e personalidade começam sem preenchimento e passam a aparecer conforme a análise das conversas. Repetir a carga atualiza os mesmos seis cadastros, preservando conversas, informações e os dados já registrados pela análise no perfil.

Durante o envio, o botão fica desabilitado e mostra **Salvando clientes…**. Sem banco configurado, a carga fica desabilitada. A tela apresenta mensagens de resultado ou validação.

Criar os cadastros não gera conversas ou análises automaticamente e não ativa os serviços fictícios. A simulação de transcrição e análise depende da configuração da aplicação.

## 7. Fluxo de análise e apresentação dos resultados

Para áudio, o Whisper realiza a transcrição local; para texto colado, essa etapa é dispensada. Com a análise real configurada, o texto é enviado ao serviço da OpenAI, cujo modelo padrão é `gpt-4o-mini`.

Os resultados alimentam resumo, tom, memória pessoal, eventos, informações financeiras, tarefas, pautas e insights para o assessor, conforme o conteúdo identificado. A análise também pode preencher o perfil financeiro ainda vazio e atualizar a personalidade, respeitando a existência de conversas mais recentes.

Sem chave da OpenAI e sem análise fictícia, a conversa preserva a transcrição sem gerar os resultados de análise. Quando serviços fictícios estão ativos, a interface sinaliza o modo de demonstração.

## 8. Roteiro de demonstração do fluxo

1. Em **Demonstração**, criar os seis clientes e abrir a carteira.
2. Abrir o perfil de um cliente e apresentar os estados iniciais.
3. Colar uma transcrição ou enviar um áudio, informando a data da conversa.
4. Acompanhar o status no histórico e, para áudio, os trechos parciais.
5. Após a conclusão, apresentar resumo, tom, memória, insights e ações que a análise tiver identificado.
6. Abrir um item para mostrar sua evidência ou contexto no modal.
7. Concluir uma tarefa ou pauta e demonstrar a possibilidade de reabri-la.
8. Abrir **Insights**, selecionar o mesmo cliente e mostrar a consulta dedicada dos sinais e o retorno ao perfil.

## Referências de implementação

- Navegação: `MemoriaRelacionamento/Pages/Shared/_Layout.cshtml`.
- Início: `MemoriaRelacionamento/Pages/Index.cshtml`.
- Carteira: `MemoriaRelacionamento/Pages/Clientes/Index.cshtml` e respectivo PageModel.
- Perfil e ações: `MemoriaRelacionamento/Pages/Clientes/Detalhe.cshtml` e respectivo PageModel.
- Consulta de insights: `MemoriaRelacionamento/Pages/Insights.cshtml` e respectivo PageModel.
- Demonstração: `MemoriaRelacionamento/Pages/Demo.cshtml` e respectivo PageModel.
- Detalhes: `MemoriaRelacionamento/Pages/Shared/_InsightCartao.cshtml`, `_DetalheModal.cshtml` e `MemoriaRelacionamento/wwwroot/js/detalhe-modal.js`.
- Progresso: `MemoriaRelacionamento/wwwroot/js/conversa-status.js` e endpoint definido em `MemoriaRelacionamento/Program.cs`.
- Validação e análise: `MemoriaRelacionamento/Services/ConversaService.cs`, `AnalysisRegistration.cs` e `ConversaProcessor.cs`.
