# 01. Ideia e Objetivos: Memória de Relacionamento

Documento atualizado em 10/10/2026, conforme o estado implementado do MVP.

## 1. Problema de negócio

Assessores de investimentos acompanham clientes com diferentes objetivos financeiros, planos de vida, preferências e necessidades. Durante as reuniões, surgem informações que ajudam a dar continuidade ao relacionamento: familiares, interesses, eventos importantes, preocupações e compromissos assumidos.

Quando esses detalhes ficam dispersos em anotações ou dependem de registro manual, preparar o próximo atendimento exige recuperar o contexto em diferentes fontes.

O problema atendido pela solução é **transformar o conteúdo das conversas em memória estruturada e consultável para os próximos atendimentos**.

## 2. Objetivo e funcionalidades do MVP

A Memória de Relacionamento registra conversas por áudio ou texto, organiza seu conteúdo com IA e apresenta os resultados no perfil do cliente.

O assessor pode:

1. Consultar a carteira e abrir o perfil de um cliente.
2. Enviar áudio WAV, MP3 ou M4A de até 200 MB, ou colar uma transcrição com pelo menos 50 caracteres, informando a data da conversa.
3. Acompanhar o processamento em segundo plano e os trechos parciais da transcrição de áudio.
4. Consultar resumo, tom da conversa, informações pessoais, eventos e informações financeiras identificadas na análise.
5. Consultar tarefas e pautas para a próxima conversa, marcando-as como concluídas ou reabrindo-as.
6. Consultar insights para o assessor, com sinais sobre apetite a risco, capacidade financeira, oportunidades de conversa e pontos de atenção.
7. Abrir detalhes para consultar evidências e contexto dos registros, além do resumo e da transcrição completa no histórico.
8. Tentar novamente o processamento de conversas com erro, quando seus dados estiverem disponíveis na máquina.

O Whisper realiza a transcrição local na máquina que executa a aplicação. O serviço da OpenAI analisa o texto com `gpt-4o-mini` como modelo padrão. Os resultados são salvos pelo processamento e exibidos diretamente, sem uma etapa de aprovação, edição ou descarte na interface.

## 3. Público atendido

**Assessor de investimentos:** consulta o histórico para preparar atendimentos, acompanhar compromissos e recuperar informações de relacionamento.

**Investidor:** é o beneficiário pretendido de conversas que considerem o contexto e os objetivos compartilhados anteriormente.

## 4. Fluxo de uso implementado

1. O assessor abre um cliente na carteira.
2. Registra a data e envia um áudio ou cola uma transcrição.
3. O sistema salva a conversa e a coloca na fila.
4. Para áudio, o Whisper gera a transcrição localmente; para texto, essa etapa é dispensada.
5. Com a análise real configurada, a OpenAI recebe o texto e o contexto disponível do cliente para produzir uma resposta estruturada.
6. A aplicação valida e normaliza os resultados e os salva junto ao estado de conclusão.
7. O perfil apresenta a memória acumulada, insights, ações e histórico.
8. O assessor consulta os detalhes e acompanha as ações para preparar o próximo contato.

A página **Insights** também permite selecionar um cliente e consultar seus sinais em uma visão dedicada, com acesso ao perfil completo. A página inicial apresenta a solução e encaminha para a carteira.

## 5. Proposta de valor

**Cada conversa vira contexto para o próximo atendimento.**

A solução reúne o que foi dito, organiza informações relevantes e mantém os próximos passos associados ao cliente. Combina a memória pessoal do relacionamento com o contexto financeiro declarado e os sinais percebidos pela IA.

Os trechos de origem disponíveis permitem ao assessor consultar a base dos registros. Insights são interpretações para apoiar a conversa, não fatos automaticamente confirmados ou recomendações de investimento.

A redução de esforço manual é um benefício pretendido. O projeto não apresenta medições que comprovem ganhos quantitativos de produtividade ou satisfação.

## 6. Perfil e insights: significado dos resultados

- **Perfil financeiro:** a análise pode preencher o campo quando ele está vazio e identifica uma declaração explícita do cliente compatível com os perfis reconhecidos. Não substitui um perfil já registrado nem realiza o processo formal de suitability.
- **Personalidade:** estimativa de estilo produzida pela IA para transcrições com pelo menos 600 caracteres. A atualização respeita a existência de conversas mais recentes.
- **Memória:** reúne informações pessoais, eventos e informações financeiras extraídas das conversas.
- **Insights:** sinais interpretativos acompanhados de uma fala verificável na transcrição. O sistema mantém no máximo quatro por análise e pode acrescentar orientação para revisar o perfil quando identifica divergência de apetite a risco.
- **Tarefas e pautas:** próximos passos consultáveis e passíveis de conclusão ou reabertura pelo assessor.

## 7. Demonstração e modos de funcionamento

A página **Demonstração** cria ou atualiza seis clientes fictícios. Os cadastros começam sem perfil financeiro ou personalidade, e a repetição da carga preserva histórico e dados já registrados pela análise. A carga não cria conversas automaticamente.

Transcrição e análise fictícias podem ser ativadas separadamente por configuração; a interface sinaliza esse modo. Sem chave da OpenAI e sem análise fictícia, a aplicação preserva a transcrição sem gerar resultados de análise.

## 8. Condições atuais de uso

O MVP é uma aplicação web sem autenticação ou separação de carteiras por assessor. O processamento utiliza um único worker ativo e os áudios ficam no disco da máquina que os recebeu.

A transcrição real depende de Windows e do modelo Whisper disponível localmente. A análise real depende de conexão com a OpenAI e chave configurada; a persistência depende de PostgreSQL/Supabase.

O áudio não é enviado à OpenAI para transcrição. A análise envia texto e contexto do cliente ao serviço externo; portanto, apenas a etapa de transcrição é local e offline.

Resultados automáticos podem conter erros ou omissões. O assessor precisa avaliar sua pertinência ao utilizá-los. A solução apoia a organização do relacionamento e não substitui a avaliação profissional, o suitability ou os processos formais da instituição.

## Referências

- Funcionalidades e execução: `README.md`.
- Arquitetura implementada: `contexto/02-arquitetura-e-tecnologia.md`.
- Navegação e telas: `contexto/03-fluxo-telas-painel.md`.
- Análise e persistência dos resultados: `MemoriaRelacionamento/Services/OpenAiConversationAnalysisService.cs` e `ConversaProcessor.cs`.
