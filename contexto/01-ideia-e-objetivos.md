# 01. Ideia e Objetivos: MVP XP Insight

## 1. O Problema de Negócio

O assessor de investimentos gerencia dezenas ou centenas de clientes, cada um com objetivos financeiros, planos de vida, preferências e necessidades particulares. Ao longo das reuniões, são compartilhadas informações relevantes para a construção de um relacionamento próximo e personalizado, como objetivos pessoais, eventos importantes, nomes de familiares, interesses e decisões discutidas anteriormente.

Após cada reunião, o assessor precisa registrar essas informações e organizar os principais pontos do atendimento, seja em um CRM, em anotações próprias ou em outros registros utilizados em sua rotina. Quando essas informações ficam dispersas ou dependem de registros manuais pouco estruturados, recuperar o contexto de uma conversa anterior pode exigir consultas adicionais e revisão de diferentes anotações.

Esse processo dificulta a preparação para os próximos contatos, aumenta o esforço operacional dedicado à organização do histórico e cria o risco de informações importantes não serem recuperadas no momento adequado. Como consequência, detalhes relevantes podem deixar de ser considerados na continuidade do relacionamento, limitando a capacidade do assessor de conduzir conversas contextualizadas e alinhadas às necessidades individuais de cada investidor.

O problema priorizado pelo MVP é, portanto, **a dificuldade de transformar as informações compartilhadas durante as reuniões em um histórico estruturado, consultável e útil para os atendimentos seguintes**.

## 2. O Objetivo do MVP

Construir um sistema funcional que utilize Inteligência Artificial para transformar gravações de reuniões com investidores em informações estruturadas, consultáveis e úteis para a continuidade do relacionamento entre o assessor e o cliente.

A solução combinará a transcrição automática de áudio, realizada localmente por meio do Whisper, com a análise semântica do texto utilizando GPT. A partir dessa análise, o sistema deverá identificar informações relevantes mencionadas durante a conversa e organizá-las em registros que auxiliem o assessor na preparação de atendimentos futuros.

A solução deverá permitir ao assessor:

1. **Importar o áudio de uma reunião:** disponibilizar a gravação para processamento pelo sistema.
2. **Transcrever o áudio automaticamente:** converter a conversa em texto por meio de processamento local para viabilizar sua análise.
3. **Extrair informações relevantes com Inteligência Artificial:** para identificar informações relevantes, preservando o contexto em que foram mencionadas.
4. **Gerar insights estruturados:** organizar objetivos declarados, preferências, eventos importantes, assuntos discutidos e ações pendentes que possam ser relevantes para o relacionamento.
5. **Revisar as informações extraídas:** permitir que o assessor avalie a exatidão e a pertinência dos resultados antes de incorporá-los ao histórico.
6. **Consultar o histórico do cliente:** armazenar e recuperar os registros aprovados, facilitando a preparação de novos atendimentos.

As informações extraídas pela Inteligência Artificial devem ser consideradas sugestões sujeitas a revisão, e não fatos automaticamente confirmados. O assessor permanece responsável por avaliar a pertinência e a exatidão dos registros utilizados no relacionamento com o cliente.

## 3. Público Impactado

**Público principal — Assessor de investimentos**

Profissional responsável pelo acompanhamento de uma carteira de clientes que precisa registrar informações relevantes, recuperar o histórico dos atendimentos e manter a continuidade do relacionamento ao longo do tempo.

**Público beneficiado — Investidor**

Cliente que poderá se beneficiar de um atendimento que considere o contexto das conversas anteriores, seus objetivos e as informações que compartilhou com o assessor.

## 4. Processo Atual e Processo Proposto

### Processo atual

1. O assessor realiza uma reunião com o cliente.
2. As informações relevantes são registradas manualmente, em anotações ou ferramentas de acompanhamento disponíveis.
3. Antes do próximo contato, o assessor consulta os registros anteriores para recuperar os principais assuntos e compromissos.
4. A preparação depende da organização, da disponibilidade e da facilidade de consulta dessas informações.

Quando os registros estão dispersos ou não seguem uma estrutura consistente, a recuperação do contexto pode demandar esforço adicional e dificultar a identificação dos pontos mais relevantes para o atendimento seguinte.

### Processo proposto com o MVP

1. O assessor importa a gravação de uma reunião.
2. O sistema transcreve o áudio e disponibiliza o texto para análise.
3. A Inteligência Artificial identifica informações relevantes e as organiza em categorias.
4. O assessor revisa as informações extraídas.
5. Os registros aprovados ficam disponíveis no perfil do cliente para consulta posterior.
6. Antes de um novo atendimento, o assessor recupera o histórico e utiliza as informações pertinentes para preparar a conversa.

O MVP busca simplificar principalmente as etapas de **organização das informações após a reunião e recuperação do contexto antes do próximo contato**. A solução complementa o trabalho do assessor, sem substituir sua avaliação profissional.

## 5. Proposta de Valor

Transformar conversas em informações estruturadas e consultáveis, ajudando o assessor a preservar o contexto do relacionamento e a preparar atendimentos futuros com base no histórico registrado.

O diferencial da proposta está em utilizar Inteligência Artificial para apoiar a organização das informações, reduzindo a dependência da recuperação exclusivamente manual de detalhes mencionados durante as reuniões.

A contribuição esperada é facilitar o acesso às informações relevantes do cliente e apoiar a continuidade do relacionamento. A efetividade desses benefícios deverá ser validada por meio de testes com o MVP, sem pressupor antecipadamente ganhos quantitativos de produtividade ou satisfação.

## 6. Critérios de Sucesso do MVP

O MVP será considerado funcional quando for possível demonstrar, em um cenário de teste controlado:

- O envio de uma gravação de reunião para processamento.
- A geração de uma transcrição utilizável.
- A extração e a organização de informações relevantes a partir da conversa.
- A revisão das informações pelo assessor antes de sua incorporação ao histórico.
- O armazenamento e a recuperação dos registros associados ao cliente.
- A utilização do histórico para identificar assuntos que podem ser retomados em um atendimento posterior.

Esses critérios avaliam o funcionamento do fluxo proposto. A comprovação de ganhos de produtividade ou de melhoria na experiência do cliente exigirá avaliações adicionais, comparando o processo anterior e o processo apoiado pela solução.

## 7. Restrições do Projeto — Fora do Escopo

Para garantir a viabilidade do MVP dentro do tempo disponível para o hackathon, as seguintes funcionalidades não fazem parte da entrega inicial:

- Autenticação de usuários ou gestão de múltiplos assessores.
- Integrações externas complexas, como CRM oficial da corretora, calendários e e-mail.
- Classificação automática do perfil de risco financeiro (*suitability*) ou recomendação de investimentos.
- Aplicação móvel ou infraestrutura distribuída complexa.

O foco da implementação será demonstrar o fluxo essencial de registro, processamento, organização e consulta das informações de relacionamento.

## 8. Segurança, Privacidade e Limites de Uso

O MVP deverá utilizar dados fictícios nos cenários de demonstração. A utilização de serviços externos de Inteligência Artificial exige atenção ao conteúdo transmitido e às condições de processamento dos dados.

As informações extraídas automaticamente podem conter erros, omissões ou interpretações inadequadas. Por isso, o assessor deverá revisar os resultados antes de utilizá-los como referência no relacionamento com o cliente.

A solução tem finalidade de apoio à organização do histórico e à preparação do atendimento. Não substitui os processos formais da instituição financeira, a avaliação profissional do assessor ou os mecanismos de controle aplicáveis ao tratamento de informações de investidores.

Como o MVP não contempla autenticação nem gestão de múltiplos assessores, ele não deve ser considerado pronto para uso produtivo com dados reais de clientes.

## 9. Evolução Futura

Após a validação do fluxo principal, a solução poderá evoluir de forma incremental, priorizando:

1. **Confiabilidade da extração:** aprimorar a identificação e a organização das informações relevantes.
2. **Experiência de consulta:** facilitar a recuperação de eventos, preferências e ações pendentes no histórico.
3. **Continuidade do relacionamento:** desenvolver recursos que auxiliem a preparação de novos contatos a partir das informações registradas.
4. **Segurança e governança:** implementar autenticação, controle de acesso, rastreabilidade e políticas de retenção de dados.
5. **Integrações corporativas:** avaliar integrações com sistemas de CRM e ferramentas de agenda, conforme viabilidade técnica e autorização institucional.

A evolução deverá ser orientada pelos resultados dos testes, pelas necessidades dos assessores e pelos requisitos de segurança e privacidade, mantendo como prioridade a utilidade prática da solução na rotina de relacionamento com o investidor.
