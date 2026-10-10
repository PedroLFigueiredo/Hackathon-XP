# Memória de Relacionamento — Hackathon XP

MVP para transformar conversas entre assessores de investimento e clientes em contexto para o próximo atendimento: família, interesses, objetivos, eventos e compromissos.

O cliente já existe no CRM. A aplicação acrescenta uma memória do relacionamento. Os insights entram automaticamente no perfil, sem etapa de aprovação.

## Stack e organização

- .NET 10 / ASP.NET Core com Razor Pages.
- Supabase/PostgreSQL com EF Core e Npgsql.
- Uma aplicação, com separação de responsabilidades por pasta. Sem microsserviços, CQRS, MediatR ou repositório genérico.
- Transcrição e análise atrás de interfaces; implementações fictícias habilitadas nesta base.

```text
HackathonXP.sln
MemoriaRelacionamento/
  Pages/                 Telas e formulários Razor Pages
  Models/                Entidades e enums, sem dependência dos serviços
  Data/                  AppDbContext e mapeamento PostgreSQL
  Services/              Contratos, criação de conversas, fila e processamento
    Fakes/               Serviços fictícios para desenvolvimento
  db/schema.sql          Estrutura do banco, sem apagar dados
  wwwroot/exemplos/       CSV padrão com seis clientes fictícios
```

As páginas criam conversas pelo `ConversaService`. O processamento depende de `ITranscriptionService` e `IConversationAnalysisService`, sem conhecer o Whisper ou um fornecedor de IA. EF Core acessa o Supabase diretamente no servidor.

## Rodar a base

Instale o SDK .NET 10 estável. O `global.json` aceita versões estáveis da família 10.0.

Na raiz do repositório:

```powershell
dotnet restore HackathonXP.sln
dotnet build HackathonXP.sln --no-restore
dotnet run --project MemoriaRelacionamento --launch-profile http --urls http://localhost:5080
```

Abra `http://localhost:5080`. Sem configurar o Supabase, as páginas iniciam e indicam a configuração pendente; não há banco em memória nem persistência simulada. `/health` verifica somente se a aplicação responde, não a conexão ao banco.

## Configurar Supabase

1. Crie um projeto Supabase e obtenha a conexão PostgreSQL do Session pooler.
2. Aplique `MemoriaRelacionamento/db/schema.sql` no SQL Editor de um banco novo. Não é uma migração dos schemas antigos descritos nos techplans.
3. Configure o segredo local, sem salvar senha no repositório:

```powershell
dotnet user-secrets set "ConnectionStrings:Supabase" "Host=SEU_HOST;Port=5432;Database=postgres;Username=SEU_USUARIO;Password=SUA_SENHA;SSL Mode=Require" --project MemoriaRelacionamento
```

Use os valores fornecidos pelo seu projeto. Em outro ambiente, pode configurar `ConnectionStrings__Supabase` por variável de ambiente.

O schema habilita RLS sem políticas públicas. Esta base usa conexão PostgreSQL no servidor, com a credencial do pooler; não usa uma chave pública Supabase no navegador. Não cria tabelas ou apaga dados automaticamente no startup.

## Preparar a demonstração

Em ambiente Development, abra `/Demo` diretamente. Essa página não aparece na navegação normal.

- Confira os seis clientes fictícios e clique em **Carregar clientes no Supabase**.
- Repetir a carga atualiza os cadastros pelo `crm_id`, sem duplicação e sem apagar o histórico.
- Clique em **Abrir clientes** e entre em um perfil.
- Cole uma transcrição com pelo menos 50 caracteres ou envie um WAV, MP3 ou M4A de até 200 MB.
- Acompanhe o status até a conclusão e consulte resumo, insights e tarefas fictícios.

Apenas os cadastros são carregados em `/Demo`. Os resultados de conversas vêm dos fakes e estão identificados como fictícios. O fake de áudio não valida nem transcreve o conteúdo do arquivo.

## Fluxo de conversa

```text
Áudio → NaFila → Transcrevendo → Analisando → Concluida
Texto → NaFila → Analisando → Concluida
Falha de processamento → Erro
```

O upload fica fora de `wwwroot`, em `uploads/`. Uma fila em memória processa um job por vez. Os estados e resultados ficam no Supabase; no reinício, o worker recupera conversas não concluídas. Resultado e status final são salvos juntos. A transcrição já salva é reutilizada.

O front acompanha `GET /api/conversas/{id}/status`, que retorna `{ "status": "Analisando", "mensagemDeErro": null }`. A consulta ocorre a cada dois segundos apenas enquanto há conversa em andamento.

## Whisper integrado

O código de `origin/main` (3ca201b) foi incorporado preservando a base .NET 10, o cadastro CRM, as telas, a demonstração e os insights automáticos. A implementação real usa Whisper.net 1.9.1 e NAudio 2.2.1 para converter WAV, MP3 e M4A em WAV mono de 16 kHz no Windows.

Por padrão, `Fakes:Transcricao=true` mantém a demonstração disponível sem modelo. Para transcrição real, disponibilize um modelo GGML compatível localmente e execute:

```powershell
dotnet run --project MemoriaRelacionamento --launch-profile http --urls http://localhost:5080 -- --Fakes:Transcricao=false --Whisper:ModelPath="D:\modelos\ggml-small.bin"
```

Caminhos relativos do modelo seguem a raiz da aplicação. O startup valida a existência do arquivo; o modelo não é baixado durante requisições. Áudios originais são preservados, temporários são removidos e cancelamento é propagado. A análise continua fictícia até integrar um provedor real.

Os testes do repositório foram adaptados à base atual: .NET 10, insights automáticos, conversas concluídas idempotentes e fila assíncrona. Para executá-los:

```powershell
dotnet test HackathonXP.sln
```

O banco SQLite é usado somente pelos testes; a aplicação continua utilizando Supabase. A normalização de áudio requer Windows. A transcrição completa ainda depende do modelo local, e o fluxo persistido depende da conexão Supabase.

### Divisão para facilitar o merge

| Frente | Arquivos principais |
|---|---|
| Front | `Pages/`, `wwwroot/` |
| Whisper | `Services/Whisper/`, `Services/TranscriptionRegistration.cs` e referências de pacote/projeto no `.csproj` |
| Base compartilhada | `Models/`, `Data/`, interfaces e pipeline; combinar alterações antes de mudar os contratos |

O `Program.cs` já chama o registro dos serviços: não precisa ser alterado para trocar o fake pelo Whisper. No merge, preservar `net10.0`, `UserSecretsId` e a referência Npgsql do projeto base. A implementação Whisper incorporada segue o contrato ITranscriptionService.

Para trabalhar apenas nas telas com o banco compartilhado, desligue a recuperação de jobs:

```powershell
dotnet run --project MemoriaRelacionamento --launch-profile http -- --Pipeline:RecuperarNoStartup=false
```

Não rode dois workers ativos sobre as mesmas conversas pendentes. Esta fila foi projetada para uma única instância no MVP.

## Status e próximas entregas

**Nesta base:** solução .NET 10, Razor Pages, entidades, mapeamento e schema Supabase, lista/perfil básicos, carga separada de seis clientes, entrada de texto/áudio, fila, acompanhamento de status, interfaces e fakes.

**Planejado:** sincronização CRM com upload/prévia CSV, exportação CSV/JSON, edição/exclusão de insights, conclusão de tarefas pela tela, painel com tarefas/datas e acabamento visual. O CSV padrão já está disponível em `/exemplos/clientes.csv`:

```csv
crm_id,nome,email,telefone
DEMO-001,Ana Martins,ana@example.com,11900000001
```

`crm_id` e `nome` são obrigatórios. Telefone é texto. A sincronização futura deve atualizar apenas o cadastro e preservar o histórico.

**Integrações pendentes:** análise real por IA. Whisper real integrado, aguardando modelo local para uso. `Fakes:Transcricao` e `Fakes:Analise` ficam habilitados por padrão; desligar um fake exige registrar sua implementação real.

Sem autenticação, múltiplos assessores, integração direta com CRM, calendário, e-mail, recomendação de investimentos ou classificação de risco neste MVP. A demonstração usa somente dados fictícios.

Os documentos em `contexto/` e `techplans/` registram o planejamento original. Alguns ainda citam .NET 8 e aprovação de insights; para esta base valem .NET 10 e insights automáticos, conforme o plano de front atualizado.

## Validação da base

- Compilação em .NET 10 sem erros ou avisos e 27 testes aprovados após a integração.
- Inicialização e respostas HTTP de início, clientes, perfil sem banco, demonstração, health e CSV.
- Endpoint de status retorna 503 quando o Supabase não está configurado.
- Mapeamento PostgreSQL, registro dos fakes, propagação de cancelamento e contrato transcrição → análise verificados sem banco externo.
- Persistência e fluxo completo com Supabase real ainda dependem de fornecer a conexão e aplicar o schema. A transcrição real precisa ser validada com um modelo local.
