# Criação de turnos: sobreposição e editor simples

Data: 19/11/2026. Pedido: impedir turnos coincidentes e simplificar a criação manual.

## Contrato de edição

`ShiftOverlapPolicy` define o bloqueio canônico; `policies/shiftOverlap.ts` implementa a mesma decisão no navegador. Um novo turno não pode sobrepor outro da mesma pessoa, ainda que a função seja diferente ou a coincidência aconteça durante o intervalo. Os limites são semiabertos: a saída de um turno pode coincidir exatamente com a entrada do outro, sem dispensar a análise de descanso e das demais regras.

A comparação usa a janela completa e os segundos civis, incluindo turnos noturnos e histórico fora do período visível. A identidade do turno em edição é excluída da comparação consigo mesmo. Pessoas diferentes podem trabalhar simultaneamente.

O bloqueio ocorre na prévia e novamente na confirmação; reconhecer outros conflitos não o contorna. Edição de saída e movimentação direta também passam pela proteção. Os comandos equivalentes de Application verificam a política antes de devolver o resultado. Nenhum endpoint novo foi acrescentado.

Importação, recuperação e abertura de documentos com sobreposição antiga continuam permitidas. Remoção e redução da interseção antiga também são permitidas. Uma edição não pode acrescentar trechos sobrepostos ao par existente. Alterações apenas cadastrais, de função, intervalo ou fixação não impedem a recuperação do planejamento por uma sobreposição herdada. Nenhum turno antigo é apagado automaticamente.

## Interface manual

O formulário destaca pessoa, entrada, saída e intervalo. Função, fixação e exibição de segundos ficam em Mais opções. Os campos têm texto maior e calendário adaptado ao tema. A mensagem de sobreposição informa pessoa e horário ocupado e desabilita Salvar turno imediatamente.

Salvar turno confere a proposta e aplica diretamente quando não há conflitos obrigatórios. Quando existem, mostra as mensagens com pessoa e data para revisão, mantém uma única ação principal e exige ciência explícita antes da confirmação permitida. Detalhes de impacto ficam recolhidos; não são exibidos códigos internos ou totais em segundos no fluxo comum. Remover turno exige confirmação e mantém Desfazer.

Alterar a exibição dos segundos não reescreve os valores existentes. A saída e a pausa continuam com datas explícitas para turnos noturnos. Ao ativar um intervalo, a sugestão inicial parte da entrada informada; os horários permanecem editáveis e passam pela validação completa, sem correção silenciosa ao salvar.

## Verificação

`shared/shift-overlap-scenarios.json` é consumido por testes C# e TypeScript e cobre 12 situações: duplicação, sobreposição parcial, contenção, pessoas diferentes, fronteira exata, um segundo, edição do próprio turno, virada noturna, conflito herdado, reparo, agravamento e troca de responsável. Testes de gravação verificam confirmação com ciência, escrita direta, movimentação, importação e atomicidade.

Validação local: 893 testes .NET aprovados (489 adaptador, 358 análise, 33 API, 13 arquitetura), 777 testes frontend aprovados em Cuiabá e 777 em Nova York, além de lint e build. A alteração final de texto foi revalidada pelos testes focados. Resultados locais são registrados em `artifacts/shift-overlap-*`. Revisão no navegador confirmou a mensagem imediata, o bloqueio da gravação e o editor simplificado. A primeira matriz completa precisou ser reiniciada porque a API local mantinha arquivos em uso; isso foi tratado como bloqueio de compilação, sem alterar os testes. A API de teste é reiniciada após a validação.

O PDF aprovado, o gerador automático e o formato nativo foram preservados. Não houve publicação ou integração; a CI remota continua necessária antes de integrar.
