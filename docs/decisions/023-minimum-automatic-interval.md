# Intervalo automático mínimo — 03/11/2026

## Cobertura do mês completo — 19/11/2026

O arquivo recebido para novembro reproduziu 90% de cobertura: entradas fixas às
07:30, 8h líquidas e pausas de 1h encerravam todos os turnos às 16:30, antes do
fechamento às 17:30. A busca ampliada não encontrava uma solução no tempo restante.

A busca mínima parcial agora entrega seu resultado de cobertura antes de gastar
tempo com preferências inferiores. A união de todos os candidatos de intervalo
mínimo fornece um limite otimista de cobertura: ignora incompatibilidades entre
candidatos, portanto nunca prova viabilidade. Quando até essa união deixa uma
lacuna, ela prova que nenhuma seleção mínima pode eliminá-la. Isso permite evitar
uma tentativa de antecipação incapaz de melhorar esse limite e, quando aplicável,
a prova nativa redundante de impossibilidade. Nos demais casos a verificação
nativa permanece obrigatória; timeout continua sem justificar ampliação.

O adaptador reutiliza presença na função somente quando todos os candidatos
presentes daquela pessoa são elegíveis à exigência. As sugestões iniciais ordenam
cobertura antes de ampliação; continuam sendo sugestões, sujeitas a todas as
restrições. Probing é desativado para que as buscas curtas sucessivas possam chegar
à procura de soluções. Prazo total de cinco segundos e um worker são preservados.
A menor ampliação total continua exigindo prova de otimalidade no modelo.

Regressões incluem o mês recebido, com nomes descritivos substituídos, 38 turnos
novos de 8h líquidas, cobertura integral, 19h totais de ampliação e histórico
preservado; limite otimista; e HTTP/worker com comparação da análise TypeScript.
Nenhuma política pura de intervalo, formato do arquivo ou PDF foi alterado.

Validação local: suíte .NET completa com 896 testes aprovados, seguida de 15
regressões de intervalo aprovadas após acrescentar a verificação do limite
otimista; 781 testes frontend; 36 testes HTTP/worker, incluindo o mês completo e
comparação C#/TypeScript. A primeira execução conjunta com a suíte frontend
concorrente esgotou o prazo mensal; a matriz .NET repetida sem essa concorrência
passou. O orçamento continua dependente da carga disponível, sem garantia geral
contra timeout. Serviço local atualizado nas portas 5003 e 5082; regeneração do
arquivo original conferida pelo serviço e análise TypeScript: 100%, zero lacunas,
48 turnos totais (38 no período e 10 históricos), versões publicadas preservadas.
Diff conferido. Sem integração ou publicação remota.

## Reforço de necessidade — 19/11/2026

Uma solução com pausas ampliadas não pode ser aceita apenas porque a primeira
tentativa com pausas mínimas terminou com timeout, ou encontrou uma solução pior.
Após encontrar melhora de cobertura, o gerador verifica novamente, com antecipação
permitida, se pausas mínimas conseguem o mesmo déficit. Se conseguem, escolhe essa
solução; se a verificação fica inconclusiva, conserva a solução mínima anterior.
Só aceita a ampliação quando essa alternativa mínima é comprovadamente inviável
no modelo e a menor ampliação total para a cobertura atingida é comprovada.
Todas as tentativas continuam dentro do orçamento nativo total de cinco segundos.

Intervalos individuais fixos, fechamento obrigatório e rotinas ancoradas por
turnos preservados continuam restrições explícitas. O gerador não reescreve o
histórico para remover esses vínculos. Os cartões mensal/diário e semanal mostram
“Intervalo maior”, duração e acréscimo quando a pausa salva excede o mínimo efetivo.
Pausas legadas sem comprovação não recebem justificativa automática de cobertura.
As políticas de mínimo C#/TS, a prévia e o formato do documento permanecem iguais.

Regressões novas cobrem duas pausas mínimas desencontradas que atendem toda a
demanda, ausência de solução mínima inicial, prazo inconclusivo, empate de
cobertura e ampliação necessária de apenas 30 minutos em vez de uma hora.

Verificação local desta revisão:

- Suíte .NET completa: 879 aprovados (477 adaptador, 356 análise, 33 API, 13 arquitetura).
- Frontend: 743 aprovados em America/Cuiaba e America/New_York; build e lint aprovados.
- Rechecagem final dos intervalos: 13 testes .NET e 19 frontend aprovados,
  incluindo o marcador de ampliação verificada e o aviso para pausas legadas.
- HTTP/worker: 33/34 passaram na execução conjunta; o cenário de contratos
  mistos não encontrou solução dentro do orçamento nesse primeiro teste e
  passou isoladamente, sem alteração de prazo ou asserções. O teste HTTP de
  intervalos passou com três dias, mínimo de uma hora e ampliação de 30 minutos.
- Serviço local recompilado/reiniciado: cenário de três dias com duas pessoas,
  seis turnos com uma hora de pausa, cobertura completa e resposta HTTP 200.
- Diff revisado. Sem integração, publicação ou execução da CI remota.

## Decisão original

Pedido: preferir o menor intervalo da empresa; ampliar somente quando reduz
lacunas. Reprodução anterior: MinimumIntervalGenerationTests encontrou 2h em vez
de 1h com cobertura parcial idêntica; ao cobrir até 17h, escolheu 45 minutos
adicionais quando 30 bastavam. Duas de cinco regressões falharam antes da correção.
Uma reprodução complementar com expediente até 17:30 e demanda até 16:30 gerou
12:30–14:30 para uma das pessoas, tanto em um dia quanto em três dias, embora
duas pausas de uma hora bastassem. Essas duas regressões também falharam antes
da correção e passaram depois.

Decisão: uma busca inicial admite somente a duração mínima efetiva (empresa,
exigência da jornada e fechamento). Uma busca com pausas ampliadas só substitui
esse resultado se diminuir estritamente o déficit canônico em ticks × pessoas.
Empate, falha ou prazo esgotado conservam a solução mínima. A ampliação não serve
apenas para ganhar bônus de fechamento ou transferir uma lacuna para outro horário.
Quando necessária, a busca ordena cobertura, menor ampliação total, crédito de
carga contratada e preferências. Não usa soma de pesos para trocar essas prioridades.

Antecipação continua sujeita à comparação de cobertura dentro de cada tentativa.
As buscas compartilham um modelo, o orçamento de construção e até cinco segundos
de busca nativa. Pausas fixas, rotinas ancoradas pelo histórico e feriados autorizados
são preservados. Limites operacionais significam que se comparam soluções
encontradas; não se promete prova de necessidade global para toda entrada.

IntervalPolicy.Extension é uma preferência de geração. O equivalente TypeScript
e os cenários compartilhados verificam mínimo da empresa, jornada, segundos,
fechamento, pausa fixa e noite. A prévia usa o mínimo efetivo, pois não faz busca
de cobertura. Análise de arquivos e edição manual não ganham erro novo. Nenhum
campo JSON, migração, regra de carga ou duração legal é alterado.

Verificação local concluída:

- `dotnet test EscalaEasy.slnx -c Release --no-restore -m:1`: 815 aprovados
  (449 adaptador, 320 puros, 33 API e 13 arquitetura).
- `node --test tests/*.test.cjs`: 609 aprovados no frontend em cada um dos fusos
  Cuiabá e Nova York. Inclui 11 cenários compartilhados C#/TS de preferência,
  mínimo na prévia e preservação do intervalo explicitamente solicitado.
- Build e lint do frontend aprovados; apenas os avisos de anotação preexistentes
  do Zod durante o build.
- `SECURITY_BUILD_CONFIGURATION=Release node --test backend/security.integration.cjs`:
  29/29 aprovados com API/worker reais. O novo caso verifica três dias, permissões
  simultâneas por função, pausas mínimas, ampliação total de apenas 30 minutos,
  rotina semanal e paridade do resultado com a análise TypeScript.
- Diff e links revisados. As expectativas antigas que esticavam o intervalo da
  prévia, ou apenas transferiam uma lacuna sem reduzir déficit, foram atualizadas
  conforme a mudança explícita de contrato.

Preparação para envio ao repositório: a comparação diferencial isolada também
passou com 191 documentos em Cuiabá e 191 em Nova York, recompilando o probe
puro atual. README e documentação do gerador foram alinhados às prioridades e à
edição de função dentro do próprio cartão; links locais e diff foram revisados.

As evidências acima são locais; a CI remota verifica o commit enviado. Não houve
nova medição de desempenho. A busca continua limitada aos candidatos e ao prazo
existentes; nenhuma garantia geral de otimalidade ou ganho de desempenho é afirmada.
