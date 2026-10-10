# 02. Arquitetura, Stack Tecnológica e Execução do MVP

## Stack Tecnológica
* **Backend / Frontend:** ASP.NET Core com Razor Pages (C#).
* **Banco de Dados:** Supabase (utilizando SDK/Client apropriado para C#).
* **Processamento de Áudio (Transcrição):** Whisper.net. **Regra rígida:** Os modelos devem ser armazenados em disco local e a transcrição tem de ser feita offline, na própria máquina.
* **Inteligência Artificial (Análise de Texto):** API da OpenAI (modelo `gpt-4o-mini`). O sistema envia a transcrição gerada pelo Whisper e pede um JSON estruturado de volta.

## Arquitetura de Software
* **Processamento:** O processamento do áudio e as chamadas à IA devem ocorrer em *background* na própria aplicação (sem filas externas complexas). A interface pode atualizar o status através de consultas simples.
* **Contratos:** Criar abstrações simples como `ITranscriptionService` e `IConversationAnalysisService`. Evitar microsserviços.

## Modelo de Dados (Tabelas no Supabase)
1. **Cliente:** Id, nome, telefone, e-mail.
2. **Conversa:** Id, cliente_id, data, caminho_do_audio, status, transcricao, resumo, sentimento (tom) e mensagem_de_erro.
3. **Informacao_Insight:** Id, cliente_id, conversa_id, categoria (ex: Família, Hobby, Objetivo), conteudo, trecho_de_origem, status_de_revisao.
4. **Acao_ToDo:** Id, cliente_id, conversa_id, descricao, data_opcional, status_de_conclusao.

## Plano de Execução (Timeline de 7 Horas)
1. **Dependências:** Instalar Whisper.net, validar modelo local e testar API do GPT-4o-mini retornando JSON.
2. **Persistência:** Ligar ao Supabase, criar tabelas e CRUD de Clientes.
3. **Transcrição:** Upload de ficheiro (WAV) na UI e processamento local.
4. **Análise:** Implementar a lógica de envio do texto para o GPT-4o-mini e extração do JSON.
5. **UI (Cliente):** Criar a visão detalhada do cliente (revisão de insights).
6. **UI (Painel):** Criar a Home com a visão geral do assessor.
7. **Polimento:** Dados fictícios para demonstração e tratamento de erros.

## Regras para a IA Geradora de Código (Claude Code)
* Trabalhar de forma incremental. Entregar código executável a cada passo.
* Se houver limitações de hardware local para o Whisper, avisar imediatamente.
* Forçar a saída da API da OpenAI para JSON estrito (ex: `response_format: { type: "json_object" }`).