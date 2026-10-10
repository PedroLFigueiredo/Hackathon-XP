# 02. Arquitetura, Stack Tecnológica e Execução do MVP

## 1. Stack Tecnológica

- **Backend e frontend:** ASP.NET Core com Razor Pages, utilizando C#.
- **Banco de dados:** Supabase PostgreSQL, acessado pela aplicação por meio de uma biblioteca compatível com C# e PostgreSQL.
- **Processamento de áudio e transcrição:** Whisper.net. Os modelos devem ser armazenados em disco local e a transcrição deve ocorrer offline, na própria máquina. O áudio não deve ser enviado a um serviço externo para transcrição.
- **Inteligência Artificial para análise textual:** API da OpenAI, utilizando o modelo `gpt-4o-mini`. A aplicação envia a transcrição para análise e solicita uma resposta em JSON estruturado.

## 2. Arquitetura de Software

### 2.1 Processamento

O processamento de áudio e as chamadas à API da OpenAI devem ocorrer em segundo plano dentro da própria aplicação ASP.NET Core, sem dependência de filas externas ou microsserviços.

A interface poderá consultar periodicamente o status da conversa para informar ao assessor se o processamento está aguardando, em andamento, concluído ou com erro.

O processamento deverá atualizar os dados da conversa ao longo das etapas, permitindo identificar falhas de transcrição, análise ou persistência.

### 2.2 Contratos e responsabilidades

A implementação deverá utilizar abstrações simples para separar responsabilidades:

- `ITranscriptionService`: recebe o arquivo de áudio e retorna a transcrição produzida localmente pelo Whisper.net.
- `IConversationAnalysisService`: recebe a transcrição e solicita à API da OpenAI a extração de informações estruturadas.
- Serviços de persistência: salvam e consultam clientes, conversas, informações extraídas, ações e insights do assessor.

A solução deve permanecer em uma única aplicação, evitando microsserviços e complexidade desnecessária para o prazo do hackathon.

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
| `data` | Data e hora da conversa, preenchida por padrão com `now()`. |
| `caminho_audio` | Caminho ou referência ao arquivo de áudio; obrigatório. |
| `tom` | Campo textual para registrar o tom identificado, se implementado. |
| `status` | Estado do processamento; obrigatório. |
| `transcricao` | Texto transcrito pelo Whisper.net. |
| `resumo` | Resumo produzido durante a análise, se disponível. |
| `mensagem_erro` | Mensagem de erro relacionada ao processamento, quando houver. |

Os valores de `status` são armazenados como texto. Os valores esperados no MVP incluem `aguardando`, `transcrevendo`, `analisando`, `concluído` e `erro`; a aplicação deve manter uma convenção consistente ao gravá-los.

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

Os estados de revisão previstos são `pendente`, `aprovado` e `descartado`.

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


### 3.6 `Ações`

Armazena próximas ações e compromissos relacionados ao cliente e à conversa.

| Campo | Descrição |
|---|---|
| `id` | Identificador UUID e chave primária. |
| `cliente_id` | Referência obrigatória ao cliente. |
| `conversa_id` | Referência obrigatória à conversa de origem. |
| `descricao` | Descrição da ação; obrigatória. |
| `data_opcional` | Data prevista, quando informada. |
| `tags` | Array de palavras-chave ou categorias. |
| `status_conclusao` | Estado da ação, com padrão `pendente`. |

Os estados de conclusão previstos incluem `pendente` e `concluido`.

### 3.7 `insights_assessor`

Armazena insights ou sugestões gerados para apoiar o assessor.

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
- As tabelas `perfil_pessoal`, `informacao_eventos` e `informacao_financeiro` mantêm o estado de revisão para apoiar a validação humana, conforme o esquema definido.
- As chaves estrangeiras configuradas com `ON DELETE CASCADE` fazem com que a exclusão de um cliente remova as conversas e os registros dependentes ligados por essas relações. Esse comportamento deve ser considerado nas rotinas de manutenção e demonstração.

## 5. Plano de Execução — Timeline de 7 Horas

1. **Dependências e validação inicial:** instalar o Whisper.net, verificar a disponibilidade do modelo local e confirmar que a API da OpenAI retorna JSON válido.
2. **Persistência:** configurar a conexão com o Supabase PostgreSQL, criar ou validar as tabelas e implementar operações básicas de cadastro e consulta de clientes.
3. **Transcrição:** criar o upload de arquivo WAV na interface e processar o áudio localmente com Whisper.net.
4. **Análise:** enviar a transcrição à API da OpenAI, validar a resposta estruturada e mapear os resultados para as tabelas correspondentes.
5. **Interface do cliente:** implementar a visualização do perfil e a revisão dos registros extraídos.
6. **Painel:** criar a página inicial com a visão geral do assessor e os acessos às informações principais.
7. **Polimento:** validar o fluxo completo com dados fictícios, apresentar estados de processamento e tratar erros esperados.

## 6. Regras para a IA Geradora de Código (Claude Code)

- Trabalhar de forma incremental e entregar código executável a cada etapa.
- Validar antecipadamente se o hardware local consegue executar o modelo Whisper escolhido; comunicar limitações de memória, desempenho ou dependências antes de investir na integração completa.
- Manter os arquivos do modelo Whisper em disco local e executar a transcrição offline.
- Solicitar respostas em JSON estruturado à API da OpenAI. Quando compatível com o endpoint e o modelo utilizados, configurar `response_format` para JSON estrito, por exemplo `{"type":"json_object"}`, e validar o conteúdo retornado antes de persistir os dados.
- Não presumir que JSON válido garante conformidade com o esquema esperado: validar campos obrigatórios, tipos e valores antes de gravar no banco.
- Não inserir chaves de API ou credenciais do Supabase diretamente no código-fonte ou no repositório.
- Utilizar exclusivamente dados fictícios na demonstração do MVP.
- Manter as responsabilidades separadas por serviços e evitar microsserviços, filas externas ou infraestrutura que não seja necessária para demonstrar o fluxo principal.

## 7. Pontos de Atenção e Limitações Conhecidas

- O esquema atual usa campos `TEXT` para estados e categorias; a aplicação deve validar os valores permitidos.
- A tabela `insights_assessor` possui RLS ativado, mas as políticas de acesso precisam ser verificadas separadamente.
- As tabelas `informacao_eventos` e `informacao_financeiro` não possuem uma coluna `conversa_id`; adicionar essa relação exige uma alteração explícita no esquema.
- A tabela `conversas` exige `caminho_audio`. A aplicação deverá garantir que esse valor seja fornecido e corresponda à estratégia de armazenamento adotada.
- A ausência de autenticação e de gestão de múltiplos assessores limita o MVP a demonstrações controladas. Não utilizar a configuração inicial como sistema produtivo para dados reais de clientes.
- O processamento em segundo plano dentro da própria aplicação atende ao escopo do hackathon, mas não equivale a uma infraestrutura durável de filas: reinícios da aplicação podem interromper tarefas em andamento, dependendo da implementação.
