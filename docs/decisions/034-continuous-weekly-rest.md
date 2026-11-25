# Repousos contínuos e rodízio de domingos

Data: 25/11/2026.

## Escopo autorizado

Conferir descanso real, mostrar folgas, explicar alertas com os turnos envolvidos
e oferecer rodízio de domingos por pessoa. Configuração de vigência e acordos
coletivos ficam expressamente fora desta entrega. A impressão PDF aprovada não
foi alterada.

## Contrato operacional

- `WeeklyRestPolicy` é a política canônica C#; `policies/weeklyRest.ts` é a
  implementação TypeScript equivalente. Ambas usam tempo civil e segundos.
- Um repouso é um bloco contínuo de 24h sem qualquer parte de um turno. Pausas
  dentro do turno não contam. Sobreposições históricas são unidas pelo maior fim.
  Um intervalo contínuo de 48h contém dois repousos.
- A meta semanal vem exclusivamente do ciclo de sete dias cadastrado: 5x2
  solicita dois repousos e 6x1 solicita um. A configuração existente não é
  convertida. Ciclos personalizados de outro comprimento e regimes em horas
  não recebem uma meta semanal inferida.
- A semana é domingo–sábado. A contagem considera blocos inteiros dentro da
  semana; não transfere partes de um bloco de uma semana para outra. É uma
  verificação operacional explícita, sem declarar enquadramento jurídico ou
  implementar compensação entre semanas.
- Semanas integralmente contidas no planejamento, com turnos salvos e pessoa
  ativa até o fim, recebem a conferência `weeklyRest`. Uma semana sem turnos,
  parcial ou atravessada pela inativação aparece como não confirmada, sem
  inventar falta de repouso. Dados ausentes fora do período não provam folga.
- Em ciclos semanais, cada janela completa de sete dias deve conter ao menos
  um bloco contínuo de 24h. `consecutiveWork` identifica uma janela que falha.
  Esse diagnóstico não é uma simples contagem das datas de início dos turnos.
- O gerador respeita a mesma política em semanas/janelas completas do documento
  que intersectam o recorte solicitado, considerando turnos preservados e
  fixados. A família Rest traduz os blocos de 24h em intervalos sem sobreposição
  com o trabalho; não altera limites, fila, isolamento ou orçamento. Quando
  todos os turnos terminam até a meia-noite da data de entrada, o ciclo existente
  já garante datas inteiras livres e dispensa restrições redundantes.
- Documentos conflitantes continuam carregáveis/editáveis. Erros novos de
  repouso participam da admissão de geração. A falha preserva os turnos anteriores.

## Interface e domingos

Folgas e repousos da equipe estão disponíveis nas visões diária, semanal e
mensal. A semana exibe o resumo por pessoa e distingue folga prevista, ausência,
turno iniciado no dia anterior e descanso não confirmado. O total de repousos
não substitui o diagnóstico de carga contratada nem comprova execução real.

No cadastro individual, Rodízio de domingos de descanso oferece não configurar
ou preferir um domingo inteiro livre a cada 2, 3 ou 4 semanas. A escolha é
explícita, sem inferência de categoria ou sexo. A frequência é persistida no
contrato existente de preferências (`sundayRest2/3/4`, peso positivo), sem nova
versão do arquivo e sem consumir o campo de descrição para regras.

O objetivo tenta reduzir janelas sem domingo livre depois da cobertura, extensão
de pausas e carga contratada, antes de preservar os horários e equilibrar outros
turnos. Preferência não vira restrição obrigatória. `sundayRotation` é aviso
quando todos os domingos da janela têm trabalho salvo; turnos de sábado que
entram no domingo também contam. Histórico ausente não é considerado folga.
O cadastro não promete conformidade com regras legais de uma categoria.

Os alertas de repouso e de descanso entre turnos oferecem Ver turnos envolvidos,
com datas, horários e acesso direto à edição; o descanso entre turnos apresenta
o mínimo efetivo da pessoa, inclusive regimes em horas.

## Validação

Os cenários em `shared/weekly-rest-scenarios.json` verificam 24h exatas, um
segundo a menos, 48h, descanso noturno de 22h, sobreposição e histórico nas
duas bordas em C# e TypeScript. Testes específicos verificam diagnósticos,
ausência de mutação, dados incompletos, ausência registrada, persistência de
preferências e rodízio com histórico. O adaptador real verifica repousos e
rodízio; a integração HTTP compara `/analyze` e `/validate` com o frontend.

### Resultados locais

| Verificação | Resultado |
| --- | --- |
| Análise/domínio puros | 360 aprovados |
| Adaptador e gerador real | 497 aprovados |
| API e arquitetura | 33 + 13 aprovados |
| Frontend, Cuiabá e Nova York | 791 aprovados em cada fuso |
| Paridade diferencial, ambos os fusos | 224 documentos em cada fuso |
| HTTP/worker | 37 aprovados, incluindo a nova comparação de repousos |
| Lint e compilação frontend/API | Aprovados |
| Edge real, desktop e celular | Folgas, contagem, salvamento do rodízio, preservação dos turnos e abertura do turno pelo alerta aprovados; sem erro de execução |
| Diff | Sem erros de whitespace |

Evidências locais: arquivos `artifacts/rest-*.txt` e
`artifacts/weekly-rest-desktop.png` / `weekly-rest-mobile.png`.
O teste visual usa documento sintético; não acessou nem alterou dados do usuário.

O baseline frontend tinha 781 testes aprovados. A primeira tentativa de baseline
.NET foi interrompida por bloqueio da comunicação entre processos no ambiente
restrito; a validação efetiva usou a execução com comunicação local permitida.
Na primeira matriz do adaptador, dois cenários perderam cobertura dentro do
prazo. A remoção da tradução redundante de repousos para turnos sem virada de
data restaurou a aprovação dos 497 testes, sem ampliar o orçamento de produção.
Isso não constitui medição de ganho de desempenho.

O build mantém os avisos existentes de tamanho de pacote e anotação do Zod.
A CI remota não foi executada e não houve commit, envio, integração ou publicação.
A meta de repouso é uma conferência operacional do ciclo cadastrado, com os
limites de semana e de histórico descritos acima; não é certificação jurídica.


## Integração autorizada

Em 25/11/2026, o usuário autorizou encerrar os processos locais, atualizar o
README e integrar todas as alterações de código, testes e documentação à main.
Os registros anteriores de ausência de envio descrevem a validação local.
A integração será realizada após aprovação da validação remota completa;
resultados temporários e documentos exportados permanecem locais.
