# 03. Fluxo de Telas e Painel UI/UX

O sistema terá duas telas principais. O design deve ser simples, focado na usabilidade do assessor.

## Tela 1: Painel Geral do Assessor (Home / Visão do Dia)
É o painel principal que o assessor abre para gerir a sua carteira de clientes.

* **Widget 1: Fila de Prioridades e Alertas de Relacionamento**
  * *Datas Importantes:* Apresenta aniversários ou eventos próximos registados na base de dados.
  * *Pautas Sugeridas (IA):* Lembretes curtos gerados para iniciar conversa (ex: "Ligar para o cliente X e perguntar sobre o novo hobby").
* **Widget 2: Central de Tarefas Pendentes (To-Do Geral)**
  * Uma lista unificada contendo todas as promessas ou ações ("Acao_ToDo") pendentes de toda a carteira de clientes, com *checkboxes* para marcar como concluído.

## Tela 2: Visão de Cliente Único (Perfil Detalhado)
Página onde o assessor foca num único cliente, faz novos uploads e visualiza o histórico extraído.

* **Ação Rápida: Upload de Reunião**
  * Área destacada para *upload* (arrastar e largar) do ficheiro de áudio da última reunião (WAV).
* **Seção 1: Resumo da Última Reunião**
  * *Sentimento / Tom:* Um indicador rápido do clima da última chamada (ex: Alinhado, Satisfeito, Necessita de atenção).
  * *Bullet Points Executivos:* 3 frases geradas pela IA resumindo a reunião.
* **Seção 2: CRM Humanizado (O Lado Pessoal)**
  * *Família:* Nomes de cônjuges, filhos e marcos citados.
  * *Estilo de Vida & Hobbies:* Interesses registados (esportes, viagens).
  * *Próximos Eventos:* Contagem regressiva para datas importantes daquele cliente.
* **Seção 3: Histórico e Ações Específicas**
  * *Revisão de Insights:* Uma área intermédia onde a IA sugere os novos dados extraídos do último áudio, permitindo ao assessor aprovar, editar ou descartar antes de salvar no perfil oficial.
  * *Linha do Tempo:* Histórico de áudios passados.
  * *To-Do Local:* Lista de tarefas exclusivas para este cliente.