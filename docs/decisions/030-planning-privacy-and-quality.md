# Planejamento: privacidade, navegação, diagnóstico e qualidade

Data: 19/11/2026. Implementação local dos itens 1, 2, 3 e 5 da revisão do produto.

## Problema e comportamento

A geração enviava o documento completo, embora nomes, categorias de ausência e versões arquivadas não participem das decisões matemáticas. `calculationDocument` constrói a entrada de cálculo sem o arquivo de versões, troca os nomes da empresa, das pessoas e dos feriados por referências neutras, esvazia descrições de preferências e substitui o nome do instrumento de compensação sem apagar sua presença ou vigência. Ausências tornam-se `unavailable`, preservando janelas e créditos explícitos. Identificadores, funções, autorizações, turnos e histórico dos dois lados permanecem. Isso reduz dados; não constitui anonimização. Rótulos de pessoas nas respostas são restaurados localmente, com substituição literal.

As chamadas direta e em fila usam essa projeção. O arquivo nativo e a recuperação continuam completos. O PDF mantém o caminho e o modelo aprovados: seus nomes são necessários para impressão. A interface explica o processamento remoto, a edição/análise local e a retenção temporária dos resultados da fila.

Após ajuste solicitado pelo usuário, a navegação principal oferece Escala do dia, Escala semanal, Escala mensal, Cobertura e Gerar Escala, com destaque permanente no botão de geração. As duas análises de cobertura continuam em opções internas. Os campos Gerar desde/Gerar até têm espaçamento explícito, cores do tema e calendário nativo adaptado ao modo escuro. Após concluir os quatro cadastros, a lateral oferece acessos compactos e expansão explícita. Explicações detalhadas do gerador ficam recolhidas. O histórico da sessão usa linguagem operacional.

Versões arquivadas voltam a ter um acesso em Dados e opções: fechamento pela política existente, reconhecimento das pendências, consulta imutável e preservação dos nomes/funções originais. Não há restauração destrutiva nem alteração da seleção usada pelo PDF. Importação, exportação, recuperação e arquivos antigos mantêm suas versões.

O resumo oferece Entender e resolver para lacunas. Cada alternativa avaliada permite abrir cadastro e localizar a pessoa na escala. Falha da busca local é distinguida de impossibilidade comprovada. O diagnóstico de geração destaca distribuições contratuais inconsistentes, mensagens reais do servidor e timeout, sem inventar uma causa única para inviabilidade global.

## Preferências de geração

`ScheduleRequest.preferences` é opcional e independente do documento nativo. Clientes antigos que não enviam esse objeto mantêm as prioridades anteriores. A interface inicia as duas opções marcadas; o usuário pode desmarcá-las a cada geração. O conversor de requisições preserva as opções nos saltos API, fila e worker.

1. Restrições obrigatórias continuam obrigatórias.
2. Cobertura, ampliação mínima dos intervalos e crédito contratual conservam sua prioridade.
3. `preserveExisting` maximiza os turnos com mesma pessoa, função, entrada, saída e pausa. IDs, origem e tags não definem mudança de horário. Turnos existentes válidos entram como candidatos opcionais com seus segundos exatos, sujeitos às restrições agregadas. Fixação continua sendo a operação para preservação obrigatória.
4. `balanceUndesirableShifts` minimiza a soma dos quadrados das contagens individuais de domingos, feriados, fechamentos e noturnos. Compara pessoas da mesma função e carga contratada de referência; sem plano semanal, usa duração diária como referência. Para noturnos, considera apenas pessoas autorizadas. Restrições e disponibilidades continuam podendo explicar diferenças.
5. As preferências anteriores são o último desempate.

Domingo e feriado usam a data civil de entrada. Feriados configurados e nacionais reconhecidos pela política existente são contados. Fechamento exige saída exatamente no fechamento da empresa, inclusive no dia seguinte. Noturno usa a política canônica existente, com fronteira exata às 22h. Um turno pode pertencer a várias categorias.

O equilíbrio soma os turnos do recorte aos 28 dias anteriores que constam no documento. Não presume histórico externo ou trabalho zero em datas sem registro. Turnos futuros fora do recorte não entram nessa preferência. Preservação tem precedência sobre equilíbrio: desmarcá-la dá mais liberdade ao rodízio. As duas opções são tentativas dentro do orçamento existente; não há garantia de melhoria, igualdade ou ótimo. A verificação excepcional de intervalos mantém a prioridade e a prova já existentes e pode consumir o tempo disponível para preferências inferiores.

Os indicadores locais mostram o período selecionado e, separadamente, os 28 dias anteriores. Não invalidam arquivos nem declaram conformidade ou justiça. Após aplicação, a mensagem informa quantas pessoas tiveram horários alterados; IDs novos, sozinhos, não inflam esse total. Desfazer e aplicação direta são preservados.

## Evidências e limites

- Matriz .NET: 892 aprovados (489 adaptador, 357 análise, 33 API, 13 arquitetura), sem ignorados; inclui solver real, segundos fora da grade, disponibilidade e opções no conversor.
- Frontend: 763 aprovados em Cuiabá e 763 em Nova York; projeção remota real nos dois caminhos, equivalência da análise, restauração literal dos rótulos, histórico e alterações por pessoa.
- Paridade diferencial: 224 documentos aprovados em Cuiabá e 224 em Nova York; a nova classificação também possui cenários compartilhados consumidos por C# e TypeScript.
- Lint e build frontend aprovados; build com avisos do empacotador sobre comentários de dependência e tamanho de bundle. Revisão no navegador verificou navegação, recolhimento, versões, impedimentos e opções.
- Integração HTTP: 34 testes existentes aprovados; teste novo de entrada reduzida, fila e precedência das opções aprovado separadamente (dois cálculos reais).
- Logs locais em `artifacts/rh-improvements-*`. Revisão visual final também verificou os indicadores e os acessos do arquivo de versões.
- Sem publicação, push ou integração. CI remota da revisão exata permanece necessária antes de integrar. Não há alegação de ganho de desempenho ou validação jurídica completa.

## Preservações

O limite de 31 dias, a passagem entre períodos, a política de jornada, os créditos de ausência, as autorizações, o worker isolado e as cotas não foram modificados. O item 4 da revisão não faz parte desta entrega. O PDF aprovado e suas dependências de saída foram preservados.
