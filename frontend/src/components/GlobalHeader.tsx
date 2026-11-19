import { useGenerationStore } from "../store/generationStore";
import { solverLabels, solverDescriptions } from "../lib/solverStatus";
import { RecoveryPanel, StorageBadge } from "./RecoveryPanel";
import { Download, FileUp, Moon, Trash2, Sun, Wifi, Printer, ChevronDown, HelpCircle, Sparkles, BookOpen } from "lucide-react";
import { PlanningPeriodEditor } from "./PlanningPeriodEditor";
import { Brand } from "./Brand";
import { UsageGuide } from "./UsageGuide";
import { VersionArchive } from "./VersionArchive";
import { exportSchedulePdf } from "../lib/schedulePdf";
import { useEffect, useRef, useState } from "react";
import { exportNative, importNative } from "../lib/api";
import { useScheduleStore, type ViewMode } from "../store/scheduleStore";
import { Badge, Button, IconButton } from "./ui";

const viewModes: { mode: ViewMode; label: string; description: string }[] = [
  { mode: "timeline", label: "Escala do dia", description: "Veja os horários de cada pessoa no dia selecionado. Clique em um nome para editar seu cadastro." }, { mode: "week", label: "Escala semanal", description: "Veja os turnos e a disponibilidade de cada pessoa na semana. Clique em um nome para editar." },
  { mode: "month", label: "Escala mensal", description: "Veja os turnos salvos e as prévias estimadas do mês. Clique no número do dia para configurar feriados; clique em um turno salvo para editar seus horários; prévias são apenas estimativas." }, { mode: "coverage", label: "Pessoas por horário", description: "Confira quantas pessoas estão escaladas e quantas são necessárias em cada período e função." },
  { mode: "heatmap", label: "Onde faltam pessoas", description: "Identifique os dias e as funções que precisam de mais pessoas na escala." }, { mode: "solver", label: "Gerar Escala", description: "Gere uma escala e revise os conflitos, as faltas de pessoas e as sugestões de ajuste." }
];



export function GlobalHeader() {
  const { document, analysis, theme, setTheme, viewMode, setViewMode, solverStatus, importDocument, clearAll } = useScheduleStore();

  const generation = useGenerationStore();
  const generationLabel = generation.active ? (generation.progress?.state === "queued" ? `Na fila · ${generation.progress.position}ª posição` : "Gerando escala…") : solverLabels[solverStatus];
  const [clearing,setClearing] = useState(false);
  const [printing,setPrinting] = useState(false);
  const [printError,setPrintError] = useState("");
  const [showUsageGuide, setShowUsageGuide] = useState(false);
  const [showVersions,setShowVersions] = useState(false);
  const group = viewMode === "solver" ? "solver" : viewMode === "coverage" || viewMode === "heatmap" ? "coverage" : "schedule";
  const [confirmation,setConfirmation] = useState("");
  const helpRef = useRef<HTMLDetailsElement>(null);
  useEffect(() => {
    const closeHelpOnOutsideClick = (event: MouseEvent) => {
      const help = helpRef.current;
      if (help?.open && event.target instanceof Node && !help.contains(event.target)) {
        help.open = false;
      }
    };
    window.document.addEventListener("click", closeHelpOnOutsideClick, true);
    return () => window.document.removeEventListener("click", closeHelpOnOutsideClick, true);
  }, []);
  return (
    <header className="app-header shrink-0 border-b border-border bg-card"><div className="header-toolbar">
      <div className="header-identity">
        <div className="header-brand-company">
          <Brand className="brand-header" />
          <h1 className="header-company" title={document.company.name}>{document.company.name}</h1>
        </div>
        <div className="header-metrics">
          <Badge>{document.company.opensAt.slice(0, 5)}-{document.company.closesAt.slice(0, 5)}</Badge>
          <Badge>{Math.round(analysis.coverageScore * 100)}% cobertura</Badge>
          <Badge title={solverDescriptions[solverStatus]}>{generationLabel}</Badge>
        </div>
      </div>



      <div className="header-actions">
        <Button disabled={printing} onClick={async () => {
          setPrintError(""); setPrinting(true);
          try { await exportSchedulePdf(document); }
          catch (error) { setPrintError(error instanceof Error ? error.message : "Não foi possível baixar o PDF."); }
          finally { setPrinting(false); }
        }}><Printer className="size-4" />{printing ? "Preparando PDF…" : "Baixar PDF"}</Button>
        {printError && <p role="alert" className="text-sm text-red-600">{printError}</p>}
        <StorageBadge />
        <details className="header-tools" onKeyDown={e => {if(e.key === "Escape") {e.currentTarget.open=false;e.currentTarget.querySelector("summary")?.focus();}}}>
          <summary><ChevronDown className="size-4" /> Dados e opções</summary>
          <div className="header-tools-content">
        <p className="flex items-center gap-1 text-xs text-muted-foreground"><Wifi className="size-3" /> Edição e análise disponíveis sem internet</p>
        <p className="text-xs text-muted-foreground">Gerar escala e baixar PDF exigem conexão com o servidor.</p>
        <Button className="w-full" onClick={() => setShowVersions(true)}>Versões arquivadas</Button>
        <Button className="w-full bg-card text-foreground dark:bg-card dark:text-foreground" onClick={() => {try {exportNative(document);} catch(error) {window.alert(error instanceof Error ? error.message : "Não foi possível exportar. Use a recuperação para baixar os dados disponíveis.");}}}>
          <Download className="size-4" />
          Exportar
        </Button>
        <label className="flex h-9 cursor-pointer items-center justify-center gap-2 rounded-md border border-border bg-card px-3 text-sm font-medium">
          <FileUp className="size-4" />
          Importar
          <input
            type="file"
            accept=".escalalivre,application/json"
            className="hidden"
            onChange={async (event) => {
              const file = event.target.files?.[0];
              if (file) {
                try {
                  const imported = await importNative(file);
                  if (window.confirm("Abrir este arquivo e substituir o planejamento atual e suas versões? Uma cópia recuperável será criada antes da substituição.")) importDocument(imported);
                } catch (error) {
                  window.alert(error instanceof Error ? error.message : "Nao foi possivel abrir o arquivo.");
                } finally {
                  event.target.value = "";
                }
              }
            }}
          />
        </label>

        <Button className="bg-card text-red-600 dark:bg-card dark:text-red-400" onClick={() => {setConfirmation("");setClearing(true);}}><Trash2 className="size-4" />Limpar tudo</Button>
        <RecoveryPanel />
        <section aria-label="Sobre o EscalaLivre" className="space-y-2 border-t border-border pt-3 text-xs leading-relaxed text-muted-foreground">
          <h2 className="font-medium text-foreground">Sobre o EscalaLivre</h2>
          <p>Gratuito, com geração matemática de escalas. O planejamento é salvo neste navegador. A geração processa horários e restrições no servidor, com nomes substituídos por identificadores. O PDF envia os nomes necessários à impressão. Resultados da geração ficam temporariamente no servidor para download.</p>
          <p>
            Com carinho,{" "}
            <a
              href="https://edsonargemon.vercel.app/"
              target="_blank"
              rel="noopener noreferrer"
              className="rounded-sm underline decoration-border underline-offset-2 transition-colors hover:text-foreground focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary"
            >
              Edson Argemon
            </a>
          </p>
        </section>
          </div>
        </details>
        <IconButton title={theme === "dark" ? "Usar tema claro" : "Usar tema escuro"} onClick={() => setTheme(theme === "dark" ? "light" : "dark")}>
          {theme === "dark" ? <Sun className="size-4" /> : <Moon className="size-4" />}
        </IconButton>
      </div>
      </div>
      <PlanningPeriodEditor key={document.id+document.planningPeriod.start+document.planningPeriod.end} />
      {showUsageGuide && <UsageGuide onClose={() => setShowUsageGuide(false)} />}
      {showVersions && <VersionArchive onClose={()=>setShowVersions(false)} />}
      {clearing && <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 p-4" onKeyDown={e => {if(e.key === "Escape") setClearing(false);}}>
        <form role="dialog" aria-modal="true" aria-labelledby="clear-title" className="w-full max-w-md space-y-4 rounded-2xl border border-border bg-card p-6 shadow-xl" onSubmit={e => {e.preventDefault();if(confirmation === "LIMPAR") {clearAll();setClearing(false);}}}>
          <h2 id="clear-title" className="text-xl font-semibold text-red-600">Apagar todos os dados?</h2>
          <p className="text-sm">Isso apaga a empresa configurada, funcionários, turnos, versões fechadas e regras de cobertura salvos neste dispositivo. A ação não pode ser desfeita. Exporte uma cópia antes, se quiser guardar seus dados.</p>
          <label className="block text-sm">Digite <strong>LIMPAR</strong> para confirmar<input autoFocus value={confirmation} onChange={e => setConfirmation(e.target.value)} className="mt-2 w-full rounded-lg border border-border bg-background p-3" /></label>
          <div className="flex justify-end gap-2"><Button type="button" onClick={() => setClearing(false)}>Cancelar</Button><Button type="submit" disabled={confirmation !== "LIMPAR"} className="bg-red-600 dark:bg-red-600 dark:text-white">Apagar tudo</Button></div>
        </form>
      </div>}
      <div className="planning-view-bar flex flex-wrap items-center border-t border-border">
      <nav aria-label="Visualizações da escala" className="header-view-nav">
        {viewModes.filter(v => v.mode !== "heatmap").map(({mode,label,description}) => {
          const active = mode === "coverage" ? group === "coverage" : viewMode === mode;
          return <button key={mode} title={description} aria-describedby="view-description" aria-current={active ? "page" : undefined}
            className={`inline-flex h-10 shrink-0 items-center gap-2 rounded-lg px-3 text-sm transition focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary ${mode === "solver"
              ? `border border-primary bg-primary font-semibold text-white shadow-md hover:brightness-110 dark:text-slate-950 ${active ? "ring-2 ring-primary ring-offset-2 ring-offset-card" : ""}`
              : `font-medium ${active ? "bg-primary/10 text-primary" : "text-muted-foreground hover:bg-muted"}`}`}
            onClick={() => setViewMode(mode)}>
            {mode === "solver" && <Sparkles className="size-4" aria-hidden="true" />}
            {mode === "coverage" ? "Cobertura" : label}
          </button>;
        })}
      </nav>
      <div className="header-view-actions">
        <button type="button" aria-haspopup="dialog" className="inline-flex h-7 items-center gap-1.5 rounded-md border border-primary/30 bg-primary/10 px-2 text-xs font-semibold text-primary transition-colors hover:bg-primary/20 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary" onClick={() => setShowUsageGuide(true)}><BookOpen className="size-3.5" aria-hidden="true" />Como usar</button>
        <details ref={helpRef} className="header-view-help">
          <summary aria-label="Ajuda sobre esta visualização"><HelpCircle className="size-4" /> Ajuda</summary>
          <p id="view-description">{viewModes.find(item => item.mode === viewMode)?.description}</p>
        </details>
      </div>
      </div>
      {group==="coverage"&&<nav aria-label="Análises de cobertura" className="flex flex-wrap gap-1 border-t border-border px-3 py-1">
        {viewModes.filter(v=>["coverage","heatmap"].includes(v.mode)).map(({ mode, label, description }) => (
          <button
            key={mode}
            title={description}
            aria-describedby="view-description"
            aria-current={viewMode === mode ? "page" : undefined}
            className={`inline-flex h-9 shrink-0 items-center gap-2 rounded-lg px-3 text-sm transition ${mode === "solver" ? `border border-primary bg-primary font-semibold text-white shadow-sm hover:brightness-110 dark:text-slate-950 ${viewMode === mode ? "ring-2 ring-primary ring-offset-2 ring-offset-card" : ""}` : `font-medium ${viewMode === mode ? "bg-primary/10 text-primary" : "text-muted-foreground hover:bg-muted hover:text-foreground"}`}`}
            onClick={() => setViewMode(mode)}
          >
            {mode === "solver" && <Sparkles className="size-4" aria-hidden="true" />}
            {label}
          </button>
        ))}
      </nav>}

    </header>
  );
}
