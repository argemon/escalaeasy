# 028 — Fechamento independente e divulgação

## Estado: PDF concluído e versão final aprovada — 19/11/2026

Após os ajustes de nome do arquivo, agrupamento da função e compactação da tabela,
o usuário aprovou o resultado e marcou a parte de PDF como concluída. O aceite
inclui a tabela compacta atual, com nome e função sem repetição desnecessária,
separação visual por funcionário e nome do arquivo no mesmo padrão do `.escalaeasy`.

O usuário aprovou expressamente o resultado atual e solicitou que a geração de
PDF não seja mais alterada. Este é o contrato final: download direto em PDF da
escala salva, agrupamento dos dias/horários/intervalos por funcionário, sem a linha
de cabeçalho anterior e com a logo EscalaEasy ao lado da paginação no rodapé.
Qualquer mudança futura nesta geração depende de um novo pedido explícito do
usuário para modificar o PDF. A orientação está registrada também no AGENTS.md
do repositório para ser observada em trabalhos futuros.

## Ajuste solicitado da tabela — 19/11/2026

Por novo pedido explícito do usuário, a função deixa de se repetir nas faixas de
datas de uma mesma pessoa. Ela aparece na primeira faixa e sempre que mudar,
inclusive ao retornar a uma função anterior. Pessoas diferentes com a mesma
função continuam identificadas individualmente. O nome permanece uma única vez.

A tabela passa a alternar o fundo por funcionário, com nome em negrito e divisória
entre pessoas, removendo as linhas horizontais internas de cada grupo. Datas,
horários, intervalos, agrupamento de dias consecutivos, fontes, cabeçalho da tabela,
logo e paginação são preservados. O padrão do nome do arquivo PDF permanece igual
ao nativo, com extensão `.pdf`.

Validação: 12 testes de PDF aprovados, incluindo regressões para faixas separadas,
funções iguais em pessoas diferentes e mudanças de função com retorno. Três PDFs
de conferência foram renderizados: tabela semelhante ao exemplo do usuário,
30 pessoas em 8 páginas e nomes/funções longos em 2 páginas. A extração confirmou
nomes/cargos sem repetição indevida, datas e mudanças de função; as páginas foram
inspecionadas visualmente. A revisão não envolve implantação ou CI remota.

### Compactação posterior solicitada — 19/11/2026

A pedido do usuário, o preenchimento vertical das células caiu de 5 para 1,5 pt
em cada lado e o espaço após os parágrafos da tabela foi zerado. O cabeçalho usa
2,5 pt em cada lado; o tamanho da fonte continua em 8,5 pt. A distância medida
entre linhas simples caiu de 24,58 para 14,58 pt (40,7%). No mesmo exemplo de
30 funcionários e 150 faixas, o PDF passou de 8 para 5 páginas. Os 12 testes de
PDF e a conferência de conteúdo foram aprovados; os exemplos compacto, extenso e
com textos longos foram renderizados e inspecionados. Sem implantação/CI remota.

## Substituição da interface — 19/11/2026

Por solicitação do usuário, o cabeçalho oferece somente “Baixar PDF”, com download
direto da escala atualmente salva no período. O painel de fechamento, seleção de
versão/destinatário, reconhecimentos e botões PNG/XLSX foi retirado. Não se cria
snapshot ao imprimir. Dados de versões anteriores continuam preservados nos
arquivos e seus contratos de importação, recuperação e undo/redo.

O PDF agrupa dias civis consecutivos com os mesmos horários, função e intervalo
por identidade de funcionário. O nome aparece apenas no primeiro grupo da pessoa.
Folgas e mudanças separam os grupos; segundos e offsets noturnos são mantidos.
Somente turnos salvos dentro do período selecionado são enviados, com projeção
sanitizada de nomes/funções/turnos. Propostas não aplicadas e prévias não são impressas.
O documento atual não recebe marca de rascunho ou versão. O texto abaixo registra
o contrato histórico, não o fluxo atual da interface.

Ajuste visual de 19/11/2026: removido o cabeçalho “ESCALAEASY / ESCALA DA EQUIPE”.
A logo vetorial do calendário e o nome EscalaEasy aparecem no rodapé de todas
as páginas, à esquerda da paginação. Exportação real renderizada e conferida;
os 10 testes de agrupamento/PDF e a compilação da API passaram.

Validação local da simplificação: 10 testes de agrupamento/PDF aprovados;
750 testes frontend aprovados. O PDF de três dias com Edson e Alex foi exportado
pela API, renderizado e inspecionado: uma página, duas linhas, cada nome uma vez,
sem marca de rascunho. Paginação com muitos funcionários e nomes longos continua
testada. O clique real em “Baixar PDF” no navegador baixou o arquivo diretamente,
sem abrir painel. A suíte geral encontrou temporariamente expectativas antigas
durante alterações simultâneas na geração; após alinhar a mensagem de uma
asserção, as verificações finais passaram. Sem commit, envio ou CI remota.

Data: 11/11/2026. Task 09. Baseline 18fc7f4; trabalho local sem commit/push.

## Decisão

Cada fechamento cria uma Publication com UUID, número sequencial no documento, período civil inclusivo e instante UTC com milissegundos. Guarda somente empresa/fuso, nomes/funções, turnos e pausas com segundos e contagens de pendências reconhecidas. Não guarda categorias/motivos de ausência, dispensas, source, tags, preferências ou configurações pessoais. Os turnos pertencem ao período pela data inicial, inclusive quando terminam no dia seguinte, conforme o contrato de edição/geração. Histórico fora do período continua no rascunho.

PublicationPolicy no Domain define validação estrutural e bloqueio de fechamento por conflitos obrigatórios/ciência incompleta; TS espelha a decisão para operação local. Cinco casos compartilhados em shared/publication-closure-scenarios.json incluem preferências reconhecidas separadamente de avisos. Outros quatro em shared/publication-preference-scenarios.json conferem folga preferida e pausa antecipada, contadas pela data civil inicial sem torná-las conflitos obrigatórios. A revisão reanalisa o documento completo, incluindo conflitos obrigatórios históricos. Pendências são resumidas em cobertura, carga, avisos e preferências; nenhuma mensagem privada é materializada no snapshot.

A coleção publications é um arquivo imutável pelas operações normais. publicationSelections é um ponteiro separado por período exato. Períodos sobrepostos têm escolhas independentes; o usuário escolhe explicitamente a versão a divulgar. Refazer fechamento cria outra versão. Edição/remoção de pessoas, ausência, cópia, geração, undo e redo conservam o arquivo atual; fechamento e mudança do ponteiro não entram no histórico do rascunho. Importação/recuperação substituem o documento inteiro explicitamente e limpam esse histórico. Coleções são copiadas nos mapeadores C# e congeladas no estado TS. Não é assinatura digital nem proteção contra edição externa do JSON.

## Persistência e cotas

Campos opcionais preservam documentos antigos sem inventar fechamentos. As migrações não normalizam nem reescrevem horários de versões fechadas. DTOs omitem pausas ausentes também no HTTP, mantendo o conteúdo igual ao arquivo/browser. Limites: 10 versões ordenadas sem buracos, 200 pessoas e 1.000 turnos por versão, 256 KiB por snapshot e 512 KiB no conjunto serializado compacto em UTF-8, além do limite nativo de 1 MiB e cotas existentes de recuperação. A confirmação valida o arquivo nativo exato antes de persistir. Nenhuma versão é descartada para liberar espaço; a orientação é exportar/arquivar e iniciar outro documento. Limpar/importar deixam explícito que substituem versões. Falha de armazenamento mantém o modo de recuperação existente.

## Saídas

O painel Fechar e divulgar permite rascunho ou versão explícita, equipe ou pessoa. Rascunho leva RASCUNHO. PDF e XLSX reaproveitam os adaptadores existentes, alimentados por um documento neutro construído exclusivamente do snapshot escolhido. A empresa/cadastro correntes não participam da exportação de uma versão fechada. PDF A4 paisagem pagina e repete cabeçalhos; XLSX usa inlineStr, sem fórmulas derivadas de texto livre. A saída pública omite Origem. Horários/pausas mantêm segundos, inclusive noite. PNG usa Canvas e páginas de 1000×1600 em ZIP sem dependência nova, com limite de 200 páginas/64 MiB; erro não baixa um arquivo parcial. Todas as saídas incluem empresa, período, identificação e legenda.

## Validação

Resultados finais e limitações em [validação geral](../tasks/operacao/VALIDACAO-FINAL.md). Testes cobrem decisão compartilhada, clone, round-trip, ausência de campos privados, limites por contagem/bytes, apontamento de vigente e preservação no histórico. Exportadores reais conferem texto seguro/XLSX e PDF. O cenário final usa loja e equipe fictícias, serviço/worker real, regras/store de produção e interface Edge para fechamento/download/undo/reabertura; não afirma repetir manualmente todos os fluxos já verificados nas tasks 01–08. Há inspeção visual PDF/PNG e conferência de paginação.

CI remota e integração continuam pendentes: a validação local Windows não equivale ao workflow Ubuntu/GitHub. Nenhum envio externo de escala é realizado.


### Apresentação dos horários — ajuste de 11/11/2026

Por solicitação do usuário, PDF/PNG/XLSX não imprimem localização de horário. A interface e as saídas formatam o instante de fechamento conforme o dispositivo, sem identificar sua localização. A escala mantém os horários registrados, sem converter turnos ao apresentar/exportar. Os metadados de compatibilidade do arquivo não se tornam rótulos visíveis. Verificação e manifesto incremental no relatório final.
