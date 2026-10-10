# 02. Arquitetura e Stack Tecnológica do MVP

Documento atualizado em 10/10/2026, conforme o código e o schema atuais.

## 1. Stack tecnológica

| Componente | Implementação atual |
| --- | --- |
| Aplicação web | C# em .NET 10, ASP.NET Core e Razor Pages. |
| Interface | HTML renderizado no servidor, CSS, Bootstrap e JavaScript para abas, modais e acompanhamento de status. |
| Persistência | Entity Framework Core com `Npgsql.EntityFrameworkCore.PostgreSQL` 10.0.3 e conexão PostgreSQL direta ao Supabase. |
| Transcrição | `Whisper.net` e `Whisper.net.Runtime` 1.9.1, com modelo local multilíngue `ggml-base.bin` por padrão. |
| Normalização de áudio | NAudio 2.2.1, usando Media Foundation no Windows. |
| Análise textual | API da OpenAI pelo endpoint `v1/chat/completions`, via `HttpClient`; modelo padrão `gpt-4o-mini`. |
| Processamento | `BackgroundService` e fila em memória com `System.Threading.Channels`. |
| Testes | xUnit e SQLite para testes de persistência; a aplicação utiliza PostgreSQL. |

O frontend está incluído em `wwwroot`, sem uma etapa de build com Node.js. A transcrição não exige o pacote Python do Whisper, FFmpeg ou chave da OpenAI. O modelo é disponibilizado previamente em disco, não baixado durante requisições.

## 2. Arquitetura e processamento

### 2.1. Organização e decisões

O projeto mantém uma única aplicação com responsabilidades separadas entre `Pages`, `Services`, `Models` e `Data`. Aplica princípios de arquitetura limpa por meio de contratos e separação de responsabilidades, sem uma divisão completa em projetos independentes de domínio, aplicação e infraestrutura.

O princípio KISS orienta a implementação: contratos para serviços substituíveis, processamento interno e consultas simples de status. A aplicação não depende de microsserviços ou filas externas.

A escolha de C# está alinhada ao uso da linguagem na XP, conforme o contexto informado pela equipe, facilitando a aproximação com seu ambiente tecnológico. Razor Pages concentra interface e backend no mesmo projeto. PostgreSQL organiza as relações entre clientes e conversas; Whisper atende à transcrição local; GPT-4o-mini é o modelo adotado para a extração estruturada de texto. Esses são critérios de projeto, sem um benchmark comparativo documentado.

### 2.2. Responsabilidades implementadas

| Componente | Responsabilidade |
| --- | --- |
| Razor Pages e seus PageModels | Exibir dados, receber formulários, validar o envio e encaminhar ações aos serviços ou ao contexto de dados. |
| `ConversaService` | Salvar texto ou áudio, validar formato e tamanho, enfileirar conversas e solicitar nova tentativa. |
| `ConversaQueue` | Manter IDs de conversas em um canal em memória com leitor único. |
| `ConversaWorker` | Recuperar pendências disponíveis na máquina e processar um job por vez, em um escopo de serviços por job. |
| `ConversaProcessor` | Coordenar transcrição, análise, atualização do perfil e persistência dos resultados. |
| `ITranscriptionService` | Contrato da transcrição real ou fictícia, incluindo acompanhamento de progresso. |
| `IConversationAnalysisService` | Contrato da análise real, fictícia ou de preservação apenas da transcrição. |
| `AppDbContext` | Mapear entidades para o schema PostgreSQL e acessar os dados. |
| `TranscriptionProgressStore` e `LocalTranscriptCache` | Manter progresso em memória e preservar transcrição completa ou parcial em disco. |

A separação é pragmática: alguns PageModels consultam e atualizam dados diretamente pelo `AppDbContext`.

### 2.3. Fluxo da conversa

1. O formulário recebe a data e um texto de pelo menos 50 caracteres, ou áudio WAV, MP3 ou M4A de até 200 MB.
2. A aplicação salva a conversa como `aguardando` e enfileira seu ID.
3. Se necessário, o áudio é normalizado para WAV mono, 16 kHz, 16 bits e transcrito pelo Whisper em português.
4. A transcrição é preservada e o status passa para `analisando`.
5. A análise recebe nome, data, transcrição, informações pessoais, eventos e informações financeiras já registrados, além do perfil financeiro cadastrado.
6. Os resultados e o status `concluído` são salvos juntos: resumo, tom, memória, tarefas, pautas, insights e alterações aplicáveis ao perfil.
7. Em falha, a aplicação tenta registrar `erro`, a etapa e uma mensagem, preservando a transcrição completa disponível.

### 2.4. Análise por IA e validação

A requisição usa `temperature: 0.2` e Structured Outputs com `response_format.type: json_schema` e `strict: true`. O modelo pode ser alterado por `OpenAI:Model`. O cliente HTTP tem timeout de 120 segundos.

Após a resposta, a aplicação desserializa e normaliza campos, categorias, datas e valores reconhecidos. Confere trechos de origem contra a transcrição após normalização de texto; citações não verificadas são removidas. Nos insights para o assessor, itens sem fala verificável são descartados, a mesma fala não sustenta dois insights e são mantidos no máximo quatro por análise.

O perfil financeiro só é aceito quando identificado como declaração explícita compatível com os perfis reconhecidos e só preenche um cadastro ainda vazio. A personalidade é estimada para transcrições com pelo menos 600 caracteres; não é atualizada quando há conversa mais recente. Uma divergência de apetite a risco pode acrescentar orientação para revisar o perfil, sem alterá-lo automaticamente.

Falhas de rede, timeout, recusa, chave inválida, limite de uso ou resposta inesperada são tratadas como erros de análise. Essas verificações não comprovam a exatidão de toda interpretação gerada pela IA.

### 2.5. Recuperação e acompanhamento

A fila é volátil, mas os estados das conversas ficam no banco. Com `Pipeline:RecuperarNoStartup=true`, o worker recupera conversas em espera, transcrição ou análise quando a transcrição está disponível ou o arquivo de áudio existe na máquina. Pendências cujo áudio está em outra máquina ficam para aquela máquina.

A transcrição completa pode ser reutilizada sem repetir o Whisper. Backups `.parcial.txt` e `.transcricao.txt` ficam junto aos áudios em `uploads/`, fora de `wwwroot`; a transcrição parcial permite consultar o trabalho preservado, mas não representa retomada do Whisper a partir do último segmento.

O PostgreSQL está configurado com até três novas tentativas em falhas transitórias. O MVP pressupõe um único worker ativo; não há coordenação distribuída entre instâncias.

O navegador consulta `/api/conversas/{id}/status` a cada dois segundos enquanto há conversas pendentes. O endpoint entrega status, erro e progresso disponível; ao detectar conclusão ou erro, a página é recarregada. `/health` verifica apenas a resposta da aplicação, sem consultar banco, Whisper ou OpenAI.
## 3. Modelo de Dados — Supabase PostgreSQL

O banco de dados está organizado em tabelas específicas para os dados dos clientes, as conversas e os diferentes tipos de informação extraída. A estrutura abaixo segue o esquema SQL definido para o projeto.

### 3.1 `clientes`

Armazena os dados básicos e os atributos de perfil do cliente.

| Campo | Descrição |
|---|---|
| `id` | Identificador UUID gerado automaticamente e chave primária. |
| `nome` | Nome do cliente; obrigatório. |
| `telefone` | Telefone do cliente. |
| `email` | E-mail do cliente. |
| `perfil_financeiro` | Perfil financeiro registrado, quando aplicável. |
| `personalidade` | Características de comunicação ou personalidade registradas. |

### 3.2 `conversas`

Armazena cada reunião processada e seu estado.

| Campo | Descrição |
|---|---|
| `id` | Identificador UUID e chave primária. |
| `cliente_id` | Referência obrigatória a `clientes.id`; a exclusão do cliente exclui suas conversas. |
| `data` | Data da conversa; a aplicação grava a data informada na interface à meia-noite UTC. O SQL tem padrão `now()`. |
| `caminho_audio` | Caminho local do áudio; para conversas de texto, a aplicação grava uma string vazia. |
| `tom` | Tom identificado na análise, mapeado para o enum de sentimento da aplicação. |
| `status` | Estado do processamento; obrigatório. |
| `transcricao` | Texto transcrito pelo Whisper.net. |
| `resumo` | Tópicos produzidos na análise, armazenados como texto separado por quebras de linha. |
| `mensagem_erro` | Mensagem de erro relacionada ao processamento, quando houver. |

Os valores de `status` são armazenados como texto. Os valores esperados no MVP incluem `aguardando`, `transcrevendo`, `analisando`, `concluído` e `erro`; o mapeamento do Entity Framework grava esses valores e também aceita `concluido` na leitura.

### 3.3 `perfil_pessoal`

Armazena informações pessoais extraídas das conversas, como interesses, preferências e aspectos de contexto pessoal.

| Campo | Descrição |
|---|---|
| `id` | Identificador UUID e chave primária. |
| `cliente_id` | Referência obrigatória ao cliente. |
| `conversa_id` | Referência obrigatória à conversa de origem. |
| `conteudo` | Informação extraída; obrigatória. |
| `trecho_origem` | Trecho da transcrição que fundamenta a informação. |
| `tags` | Array de palavras-chave ou categorias. |
| `status_revisao` | Estado da revisão, com padrão `pendente`. |

A aplicação grava `pendente` e exibe os registros diretamente. Não há fluxo de alteração desse campo na interface.

### 3.4 `informacao_eventos`

Armazena eventos relevantes para o relacionamento, incluindo datas importantes quando identificadas.

| Campo | Descrição |
|---|---|
| `id` | Identificador UUID e chave primária. |
| `cliente_id` | Referência obrigatória ao cliente. |
| `conteudo` | Descrição do evento; obrigatória. |
| `data_evento` | Data do evento, quando conhecida. |
| `trecho_origem` | Trecho da transcrição que fundamenta o registro. |
| `tags` | Array de palavras-chave ou categorias. |
| `status_revisao` | Estado da revisão, com padrão `pendente`. |

### 3.5 `informacao_financeiro`

Armazena informações financeiras mencionadas durante as conversas, sem realizar classificação automática de risco ou recomendar investimentos.

| Campo | Descrição |
|---|---|
| `id` | Identificador UUID e chave primária. |
| `cliente_id` | Referência obrigatória ao cliente. |
| `conteudo` | Informação financeira extraída; obrigatória. |
| `trecho_origem` | Trecho da transcrição que fundamenta o registro. |
| `tags` | Array de palavras-chave ou categorias. |
| `status_revisao` | Estado da revisão, com padrão `pendente`. |


### 3.6 `acoes`

Armazena tarefas e pautas relacionadas ao cliente e à conversa. A tag reservada `pauta` distingue pautas; tarefas recebem a tag `tarefa`.

| Campo | Descrição |
|---|---|
| `id` | Identificador UUID e chave primária. |
| `cliente_id` | Referência obrigatória ao cliente. |
| `conversa_id` | Referência obrigatória à conversa de origem. |
| `descricao` | Descrição da ação; obrigatória. |
| `data_opcional` | Data prevista, quando informada. |
| `tags` | Array de palavras-chave ou categorias. |
| `status_conclusao` | Estado da ação, com padrão `pendente`. |

A interface alterna o estado entre `pendente` e `concluido`.

### 3.7 `insights_assessor`

Armazena sinais interpretativos para o assessor, com os tipos `apetite_risco`, `capacidade_financeira`, `oportunidade` e `alerta`, associados à conversa de origem.

| Campo | Descrição |
|---|---|
| `id` | Identificador UUID e chave primária. |
| `cliente_id` | Referência obrigatória ao cliente. |
| `conversa_id` | Referência obrigatória à conversa de origem. |
| `tipo` | Tipo de insight; obrigatório. |
| `conteudo` | Conteúdo do insight; obrigatório. |
| `sugestao` | Sugestão associada, quando aplicável. |
| `trecho_origem` | Trecho da conversa que fundamenta o insight. |

## 4. Relações entre as Tabelas

- Um cliente pode ter várias conversas.
- Uma conversa pertence a um cliente.
- Uma conversa pode originar vários registros em `perfil_pessoal`, `acoes` e `insights_assessor`.
- Um cliente pode ter vários eventos em `informacao_eventos` e vários registros em `informacao_financeiro`.
- As tabelas `perfil_pessoal`, `informacao_eventos` e `informacao_financeiro` contêm `status_revisao`, mantido como `pendente` pelo processamento atual; esse campo não controla uma aprovação na interface.
- As chaves estrangeiras configuradas com `ON DELETE CASCADE` fazem com que a exclusão de um cliente remova as conversas e os registros dependentes ligados por essas relações. Esse comportamento deve ser considerado nas rotinas de manutenção e demonstração.

## 5. Configuração e condições de execução

- **Banco:** conexão PostgreSQL configurada no servidor por `ConnectionStrings:Supabase`, variável equivalente ou `DATABASE_URL`; URL do projeto e chaves da API Supabase não substituem essa conexão. Em Development, o leitor local carrega as chaves de banco suportadas do `.env`.
- **Schema:** as tabelas são preparadas manualmente com os scripts SQL; a aplicação não cria nem migra tabelas automaticamente. Para a tabela adicional de insights, existe `MemoriaRelacionamento/db/insights_assessor.sql`.
- **Whisper:** transcrição real habilitada por padrão, dependente de Windows e do arquivo em `Whisper:ModelPath`. A factory do modelo é compartilhada; um processor é criado para cada transcrição.
- **OpenAI:** chave em `OpenAI:ApiKey` ou `OPENAI_API_KEY`. Em Development, o leitor também aceita as chaves suportadas no `.env`. A chave e as credenciais do banco permanecem no servidor.
- **Simulação:** `Fakes:Transcricao` e `Fakes:Analise` ativam serviços fictícios separadamente. Sem chave e com análise fictícia desligada, `TranscriptOnlyAnalysisService` preserva a transcrição sem gerar fatos, resumo ou insights.
- **Demonstração:** a carga de seis clientes é explícita e repetível, com IDs fixos, preservando o histórico e os campos de perfil já preenchidos.

## 6. Características do armazenamento e acesso

Eventos e informações financeiras pertencem ao cliente e não têm coluna `conversa_id`; mantêm conteúdo, tags e trecho de origem. Informações pessoais, ações e insights do assessor também possuem vínculo com a conversa.

Pautas ficam na tabela `acoes`, distinguidas por tag. Resumos são listas na aplicação e texto separado por quebras de linha no banco. A origem da conversa é inferida de `caminho_audio`: vazio para texto, caminho local para áudio.

`insights_assessor` tem RLS habilitado pelo SQL. A aplicação acessa o banco por conexão PostgreSQL direta; o comportamento de acesso depende do usuário dessa conexão e das regras do banco. A presença de RLS nessa tabela não equivale a autenticação ou isolamento de usuários na aplicação.

O sistema não possui autenticação ou gestão de múltiplos assessores. A transcrição é local, mas o banco e a análise textual são serviços externos. A análise envia transcrição e contexto do cliente à OpenAI.

## 7. Verificação existente

O projeto `MemoriaRelacionamento.Tests` utiliza xUnit e SQLite. Os testes incluem carga de demonstração e preservação de histórico, validação de conversas, processamento e recuperação, comportamento sem análise e tratamento da resposta da OpenAI com transporte simulado.

Esses testes não equivalem a validação automática de uma chamada real à OpenAI, do desempenho do Whisper ou das permissões de um banco Supabase. O comando de execução é `dotnet test HackathonXP.sln`.

## Referências

- Dependências: `MemoriaRelacionamento/MemoriaRelacionamento.csproj`.
- Configuração e endpoints: `MemoriaRelacionamento/Program.cs`, `appsettings.json` e `Services/ServiceRegistration.cs`.
- Pipeline: `Services/ConversaService.cs`, `ConversaQueue.cs`, `ConversaWorker.cs` e `ConversaProcessor.cs`.
- Integrações: `Services/WhisperTranscriptionService.cs`, `AudioNormalizer.cs`, `AnalysisRegistration.cs` e `OpenAiConversationAnalysisService.cs`.
- Persistência: `Data/AppDbContext.cs` e `db/schema.sql`.
- Passo a passo de execução: `README.md`.
- Fluxo de telas: `contexto/03-fluxo-telas-painel.md`.