<div align="center">
  <picture>
    <source media="(prefers-color-scheme: dark)" srcset="MemoriaRelacionamento/wwwroot/images/xp-logo-light.webp">
    <source media="(prefers-color-scheme: light)" srcset="MemoriaRelacionamento/wwwroot/images/xp-logo-dark.webp">
    <img src="MemoriaRelacionamento/wwwroot/images/xp-logo-dark.webp" alt="XP Inc." width="160">
  </picture>
  <h1>Memória de Relacionamento</h1>
  <p><strong>Cada conversa vira contexto para o próximo atendimento.</strong></p>
  <p>Hackathon XP · .NET 10 · Razor Pages · PostgreSQL / Supabase · Whisper</p>
</div>

MVP em .NET 10 / ASP.NET Core Razor Pages com PostgreSQL/Supabase. O sistema cadastra clientes fictícios para demonstração e transforma conversas em contexto para o próximo atendimento.

## Identidade visual

O README utiliza os mesmos logos da aplicação, com versões para fundos claros e escuros. A interface combina cabeçalho escuro, cartões claros e destaques em verde e turquesa.

| Elemento | Referência | Uso |
| --- | --- | --- |
| Verde principal | `#165857` | Links e elementos de apoio |
| Turquesa | `#02C8BB` | Botões e destaques |
| Preto | `#121212` | Cabeçalho, títulos e texto |
| Fundo claro | `#F8F8F6` | Fundo das páginas |
| Branco | `#FFFFFF` | Cartões e conteúdo |
| Tipografia | `Interstate`, `Roboto`, `sans-serif` | Família declarada no CSS, com fallback conforme as fontes disponíveis |

Referências: [estilos da aplicação](MemoriaRelacionamento/wwwroot/css/site.css) e [logos](MemoriaRelacionamento/wwwroot/images).

## Pré-requisitos

- **Windows** para transcrição real. A conversão de áudio utiliza Media Foundation via NAudio; a aplicação verifica o sistema operacional ao iniciar com Whisper habilitado.
- **[SDK .NET 10](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)**, e não apenas o Runtime. O `global.json` permite versões estáveis da linha 10.0 a partir de 10.0.100.
- **[Git](https://git-scm.com/downloads)** para clonar o repositório.
- Banco **PostgreSQL / Supabase** com conexão PostgreSQL e permissão para criar as tabelas.
- Internet para restaurar os pacotes e baixar o modelo na primeira preparação.

Os comandos abaixo usam **PowerShell**. A implementação atual não exige Node.js, Python, o pacote Python do Whisper, FFmpeg ou chave da API OpenAI. Whisper.net, seu runtime nativo e NAudio são instalados por `dotnet restore`.

## Passo a passo para rodar

### 1. Obter o projeto

```powershell
git clone https://github.com/PedroLFigueiredo/Hackathon-XP.git
cd Hackathon-XP
dotnet --version
```

Se já possui o projeto, abra o terminal na pasta que contém `HackathonXP.sln`. A versão exibida deve ser `10.0.xxx`. Execute todos os comandos seguintes nessa pasta.

### 2. Preparar o banco

No **SQL Editor** do Supabase, execute o conteúdo de [MemoriaRelacionamento/db/schema.sql](MemoriaRelacionamento/db/schema.sql) em um banco novo, ou que ainda não tenha essas tabelas. Para PostgreSQL fora do Supabase, aplique o mesmo arquivo com seu cliente SQL.

A aplicação não cria tabelas automaticamente. O script não é uma migração: não o reaplique sobre tabelas existentes e não exclua tabelas para executá-lo. Veja os detalhes em **Banco de dados**, abaixo.

### 3. Configurar a conexão

Configure a conexão PostgreSQL com User Secrets, substituindo os valores pelos dados do seu banco:

```powershell
dotnet user-secrets set "ConnectionStrings:Supabase" "Host=SEU_HOST;Port=5432;Database=postgres;Username=SEU_USUARIO;Password=SUA_SENHA;SSL Mode=Require" --project MemoriaRelacionamento
```

Use o host, porta e usuário da conexão PostgreSQL escolhida no Supabase. A URL do projeto e as chaves da API Supabase não substituem essa conexão.

Como alternativa, crie um `.env` na raiz do repositório contendo:

```dotenv
ConnectionStrings__Supabase=Host=SEU_HOST;Port=5432;Database=postgres;Username=SEU_USUARIO;Password=SUA_SENHA;SSL Mode=Require
```

**Atenção:** o leitor de `.env` carrega apenas `ConnectionStrings__Supabase` e `DATABASE_URL`, em Development. As opções `Fakes__Transcricao`, `Fakes__Analise` e `Whisper__ModelPath` presentes no `.env.example` não são carregadas por esse leitor. Configure essas opções por variáveis de ambiente no terminal, User Secrets ou `appsettings.Development.json`.

### 4. Baixar o modelo Whisper

O modelo padrão é **`ggml-base.bin` multilíngue**, para transcrição em português. Baixe uma vez:

```powershell
New-Item -ItemType Directory -Force MemoriaRelacionamento/models | Out-Null
if (-not (Test-Path MemoriaRelacionamento/models/ggml-base.bin)) {
    Invoke-WebRequest "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-base.bin" -OutFile MemoriaRelacionamento/models/ggml-base.bin
}
Get-Item MemoriaRelacionamento/models/ggml-base.bin
```

Aguarde o download terminar. O arquivo deve ficar em `MemoriaRelacionamento/models/ggml-base.bin`, conforme `Whisper:ModelPath` em `appsettings.json`. O modelo fica fora do Git e não é baixado automaticamente pela aplicação. Se o download for interrompido, remova somente o arquivo incompleto e repita o download.

Para outro modelo GGML compatível, baixe o arquivo correspondente e configure o caminho no mesmo terminal:

```powershell
$env:Whisper__ModelPath = "models/ggml-small.bin"
```

Esse comando apenas configura o caminho; o arquivo também precisa ser baixado. Caminhos relativos partem da pasta `MemoriaRelacionamento`. Para português, use a versão multilíngue, sem o sufixo `.en`.

### 5. Restaurar dependências e compilar

```powershell
dotnet restore HackathonXP.sln
dotnet build HackathonXP.sln --no-restore
```

O frontend já está incluído em `wwwroot`; não há instalação com npm.

### 6. Iniciar e abrir no navegador

Para executar com transcrição real e análise fictícia desativada:

```powershell
$env:Fakes__Transcricao = "false"
$env:Fakes__Analise = "false"
dotnet run --project MemoriaRelacionamento --launch-profile http
```

Abra **[http://localhost:5080](http://localhost:5080)**. O perfil `http` inicia em Development e não abre o navegador automaticamente. Para parar, pressione `Ctrl+C` no terminal.

Confira a resposta da aplicação em [http://localhost:5080/health](http://localhost:5080/health): deve retornar `{"status":"ok"}`. Esse endpoint não verifica o banco.

### 7. Usar a demonstração

Abra **[Demonstração](http://localhost:5080/Demo)**, clique em **Criar 6 clientes fictícios** e depois em **Abrir clientes**. Abra um perfil e cole uma transcrição de pelo menos 50 caracteres ou envie WAV, MP3 ou M4A de até 200 MB. Acompanhe o progresso no histórico.

Com `Fakes:Analise=false`, a aplicação preserva a transcrição. A análise real para resumo, sentimento, informações e ações ainda não foi integrada.

## Rodar sem baixar o Whisper

Para apresentar as telas com transcrição e análise **fictícias**, execute no mesmo terminal:

```powershell
$env:Fakes__Transcricao = "true"
$env:Fakes__Analise = "true"
dotnet run --project MemoriaRelacionamento --launch-profile http
```

Nesse modo, o modelo não é necessário e a verificação de Windows é dispensada. O banco e suas tabelas continuam necessários para criar clientes e salvar conversas. Os resultados são simulados, inclusive ao enviar áudio. No Linux/macOS, exporte as mesmas variáveis no shell utilizado.

As variáveis valem para o terminal atual. Para voltar ao modo real, defina `Fakes__Transcricao=false` com o modelo disponível e `Fakes__Analise=false` para desativar a análise simulada.

## Problemas comuns

| Sintoma | Solução |
| --- | --- |
| `dotnet` não encontrado ou SDK incompatível | Instale o SDK .NET 10, reabra o terminal e confira `dotnet --version`. |
| Modelo Whisper não encontrado | Confira o download e `Whisper:ModelPath`, relativo à pasta da aplicação. |
| Normalização de áudio requer Windows | Use Windows para transcrição real ou `Fakes__Transcricao=true` para demonstração. |
| Banco não configurado | Configure a conexão e reinicie a aplicação. |
| Erro de conexão ou tabela inexistente | Confira credenciais, acesso de rede e execução de `db/schema.sql`. |
| Fakes ou Whisper no `.env` não fazem efeito | Use variáveis de ambiente, User Secrets ou `appsettings.Development.json`. |
| Porta 5080 ocupada | Use `dotnet run --project MemoriaRelacionamento --launch-profile http --urls http://localhost:5081` e abra a nova porta. |

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
