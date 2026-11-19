# 032 — Clareza das ausências e apresentação de datas e horários

Em 19/11/2026, a edição de funcionário exibia rótulos encostados nas datas e uma sequência de linhas diárias com datas ISO e segundos, sem distinguir claramente carga prevista de dispensa.

Os rótulos de vigência e ausência agora têm espaçamento explícito. A explicação da ausência foi simplificada; as dispensas ficam em uma seção expansível com tabela de data, carga prevista e carga dispensada, com campos identificados para leitores de tela. As regras de sobreposição continuam disponíveis em uma explicação adicional.

A apresentação das telas usa dd/MM/yyyy e HH:mm. Resumos, diagnósticos, cobertura, histórico e editores usam apresentação sem segundos. A precisão dos valores armazenados e dos cálculos permanece em segundos; abrir uma tela não altera os dados. A edição explícita de um horário passa a informar minutos. Não houve alteração no PDF aprovado, nas regras de negócio ou nos contratos de arquivo/API.

Validação: compilação TypeScript, lint, build de produção e 781 testes do frontend aprovados. Conferência em navegador local com idioma pt-BR e fuso America/Cuiaba, nas larguras de 1440 e 390 pixels, incluindo edição da dispensa e ausência de erros de página. Evidências em `artifacts/absence-display-*` e roteiro em `artifacts/check-absence-display.cjs`.

Os controles nativos de data seguem a localização do navegador; a conferência foi feita em português do Brasil. Esta entrega não inclui publicação nem integração de código.
