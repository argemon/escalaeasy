import { displayTemporalText } from "../lib/displayTime";
import { invalidContractDistributions } from "../lib/policies/contractMinimum";
import { useScheduleStore } from "../store/scheduleStore";
import { Button } from "./ui";

export function GenerationDiagnostics() {
  const {document,analysis,solverStatus,setSelectedEmployeeId} = useScheduleStore();
  if(solverStatus!=="invalid") return null;
  const failures = analysis.conflicts.filter(c=>["solverInfeasible","solverInvalid","solverTimeout"].includes(c.code));
  const invalid = invalidContractDistributions(document,document.planningPeriod.start);
  const people = new Set([...failures.flatMap(c=>c.employeeId?[c.employeeId]:[]),...invalid.map(e=>e.id)]);
  return <section aria-label="Impedimentos da geração" className="space-y-2 rounded border border-amber-500/40 p-3 text-sm">
    <h3 className="font-semibold">O que revisar antes de tentar novamente</h3>
    {failures.map((c,i)=><p key={i}>{displayTemporalText(c.message)}</p>)}
    {failures.some(c=>c.code==="solverTimeout")?<p>O prazo terminou sem confirmar uma solução. Isso não comprova que a escala seja impossível. Tente um recorte menor.</p>:<p>Confira os conflitos e a carga por pessoa abaixo. Para cada lacuna, use Entender e resolver: a avaliação mostra os impedimentos das alternativas locais testadas.</p>}
    {invalid.map(e=><p key={e.id}>{e.name}: a distribuição diária não soma a carga contratada.</p>)}
    {[...people].map(id=><Button type="button" key={id} onClick={()=>setSelectedEmployeeId(id)}>Rever {document.employees.find(e=>e.id===id)?.name??"cadastro"}</Button>)}
    <p className="text-xs text-muted-foreground">Se a mensagem não aponta uma causa específica, não há diagnóstico comprovado de uma única restrição. Os turnos anteriores foram mantidos.</p>
  </section>;
}
