# 04. Ideias Futuras: XP Insight 

Documento criado em 10/10/2026. Reúne ideias de evolução para depois da aplicação base do MVP. Nenhuma delas está implementada.

## Resumo

| # | Ideia | Valor principal | Esforço estimado | Principal risco |
| --- | --- | --- | --- | --- |
| 1 | Pré-chat gamificado com IA | Primeiro contato automático e perfil preenchido | Médio | Suitability e LGPD |
| 2 | Chat do assessor com busca por voz | Achar informação de conversas passadas rápido | Médio | Respostas sem base nos dados |
| 3 | Integração com Open Finance | Insights com base na carteira real | Alto | Regulação e consentimento |
| 4 | Notificações de ações recomendadas | Assessor não esquece o que prometeu | Baixo a médio | Excesso de alertas |

---

## 1. Pré-chat gamificado com IA

### Ideia
Antes da primeira reunião, o cliente conversa com uma IA em um chat simples. A IA faz perguntas básicas e preenche o perfil do cliente. O chat usa elementos de jogo para o cliente querer terminar.

### O que a IA coletaria
- Dados básicos: nome, contato, família, profissão.
- Interesses e hobbies.
- Objetivos de vida (casa, aposentadoria, estudo dos filhos).
- Objetivos financeiros e prazos, ditos pelo próprio cliente.
- Preferências de contato (horário, canal).

### Elementos de gamificação
- Barra de progresso do perfil (ex: "Perfil 60% completo").
- Etapas curtas, uma pergunta por vez.
- Pequenas conquistas ao completar blocos (Família, Objetivos, Estilo de vida).
- Opções de resposta em botões, além de texto livre.
- Cliente pode pular qualquer pergunta.

### Como se encaixa no sistema atual
- A conversa do chat é salva como uma `Conversa` com origem "chat".
- O texto passa pela mesma análise que já existe (memória, eventos, insights).
- O assessor revisa o que foi coletado no perfil antes da primeira reunião.

### Cuidados
- **Suitability:** o chat não pode classificar perfil de risco nem recomendar investimento. Só registrar o que o cliente declara. Isso segue a regra atual do projeto.
- **LGPD:** pedir consentimento claro no início e explicar para que os dados serão usados.
- O cliente precisa de acesso ao chat. Hoje o sistema não tem autenticação nem área do cliente. Será preciso um link seguro e temporário, ou login simples.
- A IA deve avisar que é uma IA e que o assessor vai revisar as informações.

### Etapas sugeridas
1. Chat só com texto e perguntas fixas.
2. IA conduz a conversa de forma dinâmica.
3. Gamificação (progresso e conquistas).
4. Link seguro enviado ao cliente.

---

## 2. Chat do assessor para consultar conversas passadas (com áudio)

### Ideia
O assessor faz perguntas em linguagem natural e recebe respostas baseadas nas conversas já registradas. Também pode perguntar falando, e ouvir a resposta.

### Exemplos de perguntas
- "O que o Roberto disse sobre a cirurgia da esposa?"
- "Quais promessas eu fiz para a Fernanda?"
- "Quais clientes têm aniversário este mês?"
- "Quem comentou sobre viagem para Portugal?"

### Como funcionaria
1. O assessor fala ou digita a pergunta.
2. Se for voz, o Whisper transcreve (já existe no projeto).
3. O sistema busca as informações no banco: memória, eventos, tarefas, insights e transcrições.
4. A IA monta a resposta usando **somente** esses dados.
5. A resposta mostra a fonte (conversa, data, trecho), como já acontece nos modais.
6. Opcional: resposta em áudio (síntese de voz).

### Pontos técnicos
- Começar com busca simples por cliente + filtros nas tabelas atuais.
- Evoluir para busca semântica (embeddings) nas transcrições. Pode usar pgvector no Supabase/PostgreSQL.
- A IA precisa responder "não encontrei" quando não há dado, sem inventar.
- Limitar a busca ao cliente citado, para reduzir erro e custo.

### Cuidados
- Mostrar sempre a origem da resposta, para o assessor poder verificar.
- Áudio da pergunta pode ter dados sensíveis. Definir se é salvo ou descartado.
- Se a síntese de voz usar serviço externo, isso envia texto para fora. Avaliar com a regra de privacidade do projeto.

---

## 3. Integração com Open Finance

### Ideia
Com autorização do cliente, trazer dados reais da carteira (contas, investimentos, movimentações) e cruzar com o que foi dito nas conversas.

### O que isso permitiria
- Ver patrimônio e distribuição real dos investimentos.
- Comparar o que o cliente diz com o que ele tem. Exemplo: cliente diz ser agressivo, mas tem quase tudo em renda fixa.
- Alertar quando há mudança grande (saque alto, nova dívida, aporte parado).
- Insights mais específicos: ligar objetivo declarado ("juntar 500 mil até 2030") com o saldo real.
- Confirmar liquidez disponível quando surge uma necessidade (ex: cirurgia de 90 mil).

### Como se encaixa no sistema atual
- Nova tabela de posições/saldos ligada ao `Cliente`.
- O contexto enviado à análise inclui um resumo da carteira.
- Novos tipos de insight, como "Divergência entre fala e carteira".
- Nova seção no perfil: "Carteira (Open Finance)".

### Cuidados importantes
- **Regulação:** Open Finance no Brasil funciona com instituições autorizadas pelo Banco Central e consentimento do cliente. É provável precisar de um parceiro ou agregador já autorizado. Verificar isso antes de planejar prazo.
- **Consentimento:** o cliente autoriza, tem prazo e pode revogar. O sistema precisa tratar isso.
- **Segurança:** dados financeiros reais exigem autenticação, separação por assessor e proteção de dados. O MVP atual não tem nada disso.
- **Privacidade da IA:** enviar dados de carteira para a OpenAI exige decisão clara. Pode ser melhor enviar só um resumo, sem identificar o cliente.
- **Escopo:** manter a regra de não recomendar investimento. Os insights apoiam a conversa, não indicam produto.

### Etapas sugeridas
1. Definir parceiro/agregador e requisitos legais.
2. Criar autenticação e separação de carteiras (pré-requisito).
3. Importar saldos e posições.
4. Cruzar com os insights existentes.

---

## 4. Notificações de ações recomendadas

### Ideia
As tarefas e pautas já existem. A melhoria é o sistema avisar o assessor na hora certa, em vez de ele precisar abrir o perfil para ver.

### Tipos de notificação
- **Prazo chegando:** "Enviar proposta ao Roberto amanhã".
- **Tarefa atrasada:** promessa passou da data.
- **Datas importantes:** aniversário do cliente ou de familiares nos próximos dias.
- **Cliente sem contato:** muito tempo sem conversa registrada.
- **Ponto de atenção:** conversa com tom "Requer atenção" sem tarefa de retorno.
- **Resumo diário:** lista do dia ao abrir o sistema ou por e-mail.

### Canais possíveis
1. Aviso dentro do sistema (sino no cabeçalho). Mais simples.
2. E-mail diário.
3. Notificação push ou WhatsApp. Mais complexo.

### Como se encaixa no sistema atual
- Tabela de notificações (tipo, cliente, tarefa, data, lida/não lida).
- Um serviço em segundo plano, como o worker atual, que verifica prazos e datas periodicamente.
- Página ou painel "Hoje" com o que precisa de ação.
- Marcar como lida, adiar ou concluir a tarefa pela própria notificação.

### Cuidados
- Evitar excesso de avisos. Agrupar e priorizar, senão o assessor ignora.
- Deixar o assessor escolher quais tipos receber.
- Com mais de um assessor, cada um recebe só os seus clientes (depende de autenticação).

---

## Ordem sugerida

1. **Notificações** (4): usa dados que já existem e dá valor rápido.
2. **Chat do assessor** (2): reaproveita Whisper e os dados atuais.
3. **Pré-chat gamificado** (1): precisa de acesso do cliente e cuidado com LGPD.
4. **Open Finance** (3): maior impacto, mas depende de regulação e segurança.

## Pré-requisitos em comum

- Autenticação de usuários e separação de carteiras por assessor.
- Política de privacidade e consentimento (LGPD).
- Decisão sobre quais dados podem ser enviados a serviços externos.
- Manter a regra atual: o sistema apoia o relacionamento e **não** recomenda investimentos nem faz suitability.
