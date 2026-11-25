# Fila de exportação

Data: 25/11/2026.

## Problema e autorização

PDF/XLSX eram preparados no processo HTTP. O usuário solicitou fila como a de
geração, com posição, espera e cancelamento. A autorização inclui o transporte
do download; conteúdo, aparência, agrupamento e nome do PDF permanecem preservados.

## Contrato

- `POST /export/jobs` valida e copia o pedido; retorna 202 e ticket aleatório
  de 256 bits. `GET` e `DELETE /export/jobs/{id}` acompanham e cancelam.
- Fila FIFO independente da geração: uma exportação por vez e até 20 aguardando.
  Não há o limite de cinco segundos de espera proposto anteriormente. A
  estimativa é informativa. A página precisa permanecer aberta para acompanhar.
- `Baixar PDF` permanece uma ação direta. O arquivo representa a escala salva
  no momento do clique, mesmo após editar ou trocar de aba durante a espera.
  Estado global mantém o acompanhamento; cliques repetidos ficam desabilitados.
- Reconexão conserva o ticket e não repete o POST. Falha na confirmação inicial
  informa a incerteza em vez de reenviar automaticamente.
- Cancelamento na espera libera a reserva; durante execução, a vaga só é
  liberada depois de encerrar e aguardar o filho. Cancelamento tardio não
  transforma uma exportação já concluída em cancelada.
- PDF/XLSX em `/export` usam a mesma fila e cota, preservando compatibilidade.
  O contrato de importação/exportação nativa permanece existente.
- Estados finais e resultados expiram após dois minutos. Reiniciar a API perde
  a fila. Tickets são segredos e não são registrados nos logs da aplicação.

## Limites e isolamento

| Controle | Padrão |
| --- | --- |
| Em execução | 1 |
| Aguardando | `ExportJobQueueLimit=20` |
| Exportações por IP/minuto | `ExportsPerMinute=6`, somando rotas antiga e nova |
| Tempo após sair da fila | `ExportWorkerTimeoutSeconds=15` |
| Memória residente observada do filho | `ExportWorkerMemoryMb=512` MiB |
| Arquivo gerado | 10 MiB |
| Reserva ativa / payload serializado máximo | 16 MiB |
| Orçamento de reservas/resultados | `ExportJobPayloadBudgetMb=384` MiB |
| Tickets em memória | `ExportJobRetentionLimit=128` |
| Retenção final | 2 minutos |

As opções pertencem a `Protection` e são validadas ao iniciar. A reserva de
16 MiB é substituída pelo resultado JSON UTF-8 ao concluir. O orçamento inclui
resultados retidos, podendo recusar antes de atingir 20 aguardando. Não remove
resultados antecipadamente para liberar vagas. Grafos e buffers temporários
ficam fora da conta de payload.

`ExportWorker` executa argumento fixo `--export-worker`, sem shell ou dados nos
argumentos. Usa os mesmos serializer/renderizadores PDF/XLSX, com pipes limitados.
Timeout, cancelamento, excesso de memória ou saída encerram o filho antes de
liberar a vaga. A memória é observada a cada 50 ms e admite picos; não substitui
limite rígido no host. Geração e exportação podem ter um filho cada.

Consultas e cancelamentos compartilham as respectivas cotas de tickets da
geração. Exportar também consome a cota geral. Metadados de rota resolvida
preservam as cotas diante de caixa/barra final/IP falso. O proxy também deve
evitar registrar tickets em seus logs.

## Validação

Testes verificam FIFO, espera além de cinco segundos com relógio controlado,
cancelamento, ausência de paralelismo, reservas, retenção, expiração, orçamento,
formato inválido e recuperação após resultado excessivo. HTTP verifica tickets,
PDF/XLSX reais, cancelamento e cota compartilhada entre rotas, caixa/barra/IP falso.

Frontend verifica reconexão sem reenviar, cancelamento sem download, cliques
repetidos, entrada excessiva, erros e preservação do snapshot/nome. Navegador
verifica posição, navegação, cancelamento, download e desktop/celular.

`SchedulePdfExporter.cs` e `SchedulePdfRows.cs` não foram alterados. Regras de
escala, formato `.escalalivre` e cadastros não receberam mudanças nesta entrega.
Resultados finais e limitações da execução são registrados abaixo.

### Resultado local

- Backend: 497 testes do adaptador, 360 puros, 44 da API e 13 de arquitetura
  aprovados, totalizando 914.
- Frontend: 794 testes completos e dois testes novos do store aprovados em
  Cuiabá; matriz completa de 796 aprovada em Nova York. Lint e build aprovados.
- HTTP: 39 cenários executados, 38 aprovados inicialmente. O cenário existente
  de contratos mistos perdeu cobertura durante execução concorrente com testes
  frontend; foi reexecutado isoladamente e passou, junto de três cenários de
  exportação/segurança. Não se alteraram os limites do gerador para acomodá-lo.
- Edge: fila, posição, botão indisponível durante o pedido, navegação, desktop,
  celular, cancelamento, preservação do documento e nome do download aprovados.
- Documento sintético estruturalmente válido com 200 pessoas e 6.200 turnos,
  sem agrupamento de dias consecutivos: entrada de 1.748.481 bytes, PDF de
  1.439.709 bytes, concluído em 5.309 ms sob os limites de 15s/512 MiB. Trata-se
  de uma verificação desse caso local, não de garantia de capacidade da VPS.
- Diff sem erros de whitespace. Os avisos existentes de tamanho de pacote e
  anotação de biblioteca no build permanecem.

Evidências locais em `artifacts/export-queue-*.txt`,
`artifacts/export-queue-desktop.png` e `artifacts/export-queue-mobile.png`.
Nenhuma CI remota, envio, integração ou publicação foi realizado nesta entrega.


## Integração autorizada

Em 25/11/2026, o usuário autorizou encerrar os processos locais, atualizar o
README e integrar todas as alterações de código, testes e documentação à main.
Os registros anteriores de ausência de envio descrevem a validação local.
A integração será realizada após aprovação da validação remota completa;
resultados temporários e documentos exportados permanecem locais.
