# Memória de Relacionamento — Hackathon XP

MVP em .NET 10 / ASP.NET Core Razor Pages com PostgreSQL/Supabase. O sistema cadastra clientes fictícios para demonstração e transforma conversas em contexto para o próximo atendimento.

## Executar

Instale o SDK .NET 10. Na raiz do repositório:

```powershell
New-Item -ItemType Directory -Force MemoriaRelacionamento/models | Out-Null
Invoke-WebRequest "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-base.bin" -OutFile MemoriaRelacionamento/models/ggml-base.bin
dotnet restore HackathonXP.sln
dotnet build HackathonXP.sln
dotnet run --project MemoriaRelacionamento --launch-profile http --urls http://localhost:5080
```

## Banco de dados

A aplicação usa o schema enviado pelo usuário: clientes, conversas, perfil_pessoal, informacao_eventos, informacao_financeiro e acoes. A vírgula ausente após email TEXT foi corrigida em schema.sql e MemoriaRelacionamento/db/schema.sql.

Aplique MemoriaRelacionamento/db/schema.sql no SQL Editor do Supabase em um banco novo ou que ainda não tenha essas tabelas. Esse arquivo não migra as tabelas singulares da base anterior; não exclua tabelas existentes para aplicá-lo. A aplicação não cria tabelas automaticamente.

Configure a conexão PostgreSQL do Supabase no servidor:

```powershell
dotnet user-secrets set "ConnectionStrings:Supabase" "Host=SEU_HOST;Port=5432;Database=postgres;Username=SEU_USUARIO;Password=SUA_SENHA;SSL Mode=Require" --project MemoriaRelacionamento
```

Também é possível usar a variável ConnectionStrings__Supabase ou DATABASE_URL no formato postgresql://usuario:senha@host:5432/postgres. Em Development, a aplicação lê essas chaves do .env na raiz do repositório ou da aplicação; a conexão explícita em configuração tem prioridade. Senhas com caracteres especiais na URL devem usar codificação de URL. O .env permanece ignorado pelo Git. Reinicie a aplicação após alterar a conexão. Sem conexão configurada, as telas mostram um aviso e a carga fica desabilitada. Credenciais não são enviadas ao navegador.

## Demonstração

1. Abra /Demo pelo menu **Demonstração**.
2. Confira os seis cadastros e clique em **Criar 6 clientes fictícios**.
3. Clique em **Abrir clientes** e abra um perfil.
4. Cole uma transcrição de pelo menos 50 caracteres ou envie WAV, MP3 ou M4A de até 200 MB.
5. Acompanhe a porcentagem e o texto parcial no histórico; a transcrição completa fica disponível ao concluir.

A carga inclui nome, telefone, e-mail, perfil financeiro e personalidade. IDs fixos evitam duplicação ao repetir o clique, atualizam os cadastros e preservam conversas e informações. A carga só acontece ao clicar no botão; não inventa conversas nem resultados de análise.

O schema não contém crm_id, criado_em ou pautas_sugeridas. A origem da conversa é inferida do caminho de áudio; conversas de texto usam caminho vazio para respeitar NOT NULL. O resumo é salvo como texto e os estados usam aguardando, transcrevendo, analisando, concluído e erro. Categorias pessoais são armazenadas em tags; eventos e objetivos financeiros usam suas respectivas tabelas. status_revisao permanece pendente, conforme o padrão enviado; a tela mostra os dados sem uma etapa de aprovação.

O antigo CSV em wwwroot/exemplos/clientes.csv é um exemplo legado de CRM e não alimenta a tela de demonstração.

## Processamento

Uma fila em memória processa um job por vez. O worker recupera conversas pendentes ao reiniciar. Os resultados e o estado final são salvos juntos. Áudios ficam em uploads/, fora de wwwroot. O endpoint /api/conversas/{id}/status permite acompanhar o estado.

A transcrição real está habilitada por padrão, com Whisper:ModelPath=models/ggml-base.bin. O modelo é baixado uma vez pelos comandos acima e permanece fora do Git. Whisper.net 1.9.1 e NAudio 2.2.1 fazem a transcrição e a conversão de áudio; a normalização requer Windows. O modelo não é baixado durante requisições.

Fakes:Transcricao=true e Fakes:Analise=true habilitam explicitamente os serviços fictícios da demonstração. Com Fakes:Analise=false, a aplicação preserva a transcrição sem inventar resumo, sentimento, informações ou ações; a análise real ainda não foi integrada.

Os trechos transcritos são preservados em uploads/ como arquivos .parcial.txt; a transcrição completa é preservada em .transcricao.txt antes do salvamento no banco. Uma tentativa novamente reutiliza o texto completo quando disponível, sem repetir o Whisper. Conversas com erro exibem a etapa da falha e o botão **Tentar novamente**. Falhas transitórias de conexão com o PostgreSQL têm até três novas tentativas automáticas.

Para trabalhar nas telas sem recuperar jobs do banco compartilhado, configure Pipeline:RecuperarNoStartup=false. O MVP foi projetado para um único worker ativo.

A aplicação não tem autenticação. A página /Demo está acessível nos ambientes de execução para apresentação; a publicação deve considerar controle de acesso conforme o contexto de uso.

## Verificação

```powershell
dotnet test HackathonXP.sln
```

SQLite é usado somente nos testes. Os testes cobrem criação e repetição da carga de seis clientes, preservação do histórico, leitura de eventos e informações financeiras, processamento e fila. A aplicação utiliza PostgreSQL/Supabase. /health verifica a resposta da aplicação, sem testar o banco.

Os documentos em contexto/ e techplans/ registram o planejamento anterior; os nomes de tabelas e a carga por crm_id ali descritos foram substituídos pelo schema enviado.
