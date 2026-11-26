# Importacao E Exportacao

EscalaEasy é local-first. A persistência principal é um arquivo `.escalalivre` com JSON estruturado. A extensão, o schema e os identificadores de formato existentes são mantidos para compatibilidade; não é necessário converter ou renomear arquivos antigos. PDF e PNG passam a identificar a marca como EscalaEasy; os dados da empresa, os nomes de download e os formatos de exportação permanecem iguais.

## Arquivo nativo

O download usa o padrão `Nome da empresa - DD-MM-AAAA a DD-MM-AAAA.escalalivre`, com as datas inicial e final do período selecionado, inclusive quando não há turnos. Exemplo: `Minha Empresa - 01-11-2026 a 30-11-2026.escalalivre`. O separador é hífen porque `>` não é permitido em nomes de arquivo no Windows. Acentos são preservados; caracteres proibidos são substituídos por hífen. A mesma convenção vale para exportar o planejamento disponível e baixar cópias nativas de recuperação, usando os dados da própria cópia.

O botão **Baixar PDF** usa a mesma convenção, trocando apenas a extensão: `Minha Empresa - 01-11-2026 a 30-11-2026.pdf`. O nome é calculado pela mesma função da exportação nativa. Essa alteração não modifica o conteúdo, a aparência nem a geração do PDF. Verificação: testes de nomes nativos e exportação do PDF.

Essa alteração afeta apenas o nome do download no navegador. O conteúdo JSON e as regras da escala permanecem iguais. A verificação inclui exportação sem turnos, período entre anos, reimportação, caracteres proibidos, compilação, lint e testes de recuperação.

O arquivo nativo deve reconstruir completamente o estado:

- `schemaVersion`
- `id` UUID
- `company`
- `employees`
- `assignments`
- `updatedAt`

Todas as datas usam ISO-8601, todos os nomes usam camelCase e todos os identificadores sao UUID.

## XLSX

XLSX e uma exportacao humana para visualizacao e conferencia. A importacao XLSX deve ser parcial e mapeada para funcionarios, disponibilidade ou turnos, nunca como fonte completa de verdade.

## PDF

PDF e somente impressao/publicacao. Nao deve ser usado para reconstruir estado.

O PDF do backend é renderizado por PDFsharp/MigraDoc 6.2.4 (MIT), com fontes Noto Sans incorporadas (SIL OFL), páginas A4 horizontais, cabeçalho de tabela repetido e numeração. Inclui todos os turnos salvos, com funcionário, função, datas e horários civis, intervalo e origem. A renderização não depende das fontes instaladas no servidor. A impressão mensal do navegador continua disponível pela interface.
