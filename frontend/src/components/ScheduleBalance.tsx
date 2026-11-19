import { useMemo } from "react";
import { qualitySummary, qualityCategories, type QualityCategory } from "../lib/scheduleQuality";
import { useScheduleStore } from "../store/scheduleStore";

export function ScheduleBalance() {
  const document = useScheduleStore(s=>s.document);
  const summary = useMemo(()=>qualitySummary(document),[document]);
  const groups = [...new Set(summary.people.map(p=>p.group))];
  return <details className="rounded border border-border p-3"><summary className="cursor-pointer font-semibold">Equilíbrio entre pessoas</summary>
    <p className="my-2 text-xs text-muted-foreground">Turnos do período e, entre parênteses, os 28 dias anteriores a ele que constam neste arquivo. Ausência de histórico não significa ausência de trabalho. Domingos e feriados são contados pela data de entrada; fechamento é a saída no fechamento da empresa. Um turno pode entrar em várias colunas.</p>
    {groups.map(group=><div key={group} className="my-3 overflow-x-auto"><p className="text-sm font-medium">{summary.people.find(p=>p.group===group)?.role} · {summary.people.find(p=>p.group===group)?.reference}</p><table className="w-full text-left text-xs"><caption className="sr-only">Distribuição de turnos por pessoa</caption><thead><tr><th className="p-2">Pessoa</th>{Object.values(qualityCategories).map(label=><th className="p-2" key={label}>{label}</th>)}</tr></thead><tbody>{summary.people.filter(p=>p.group===group).map(p=><tr className="border-t border-border" key={p.id}><th scope="row" className="p-2"><button className="underline" onClick={()=>useScheduleStore.getState().setSelectedEmployeeId(p.id)}>{p.name}</button></th>{(Object.keys(qualityCategories) as QualityCategory[]).map(c=><td className="p-2" key={c}>{p.current[c]} ({p.history[c]})</td>)}</tr>)}</tbody></table></div>)}
    {!summary.people.length&&<p className="text-sm">Cadastre pessoas para comparar a distribuição.</p>}
    <p className="text-xs text-muted-foreground">A comparação considera função e carga de referência. Disponibilidades, autorizações e dias de trabalho podem justificar diferenças. Este indicador não classifica a escala como justa ou irregular.</p>
  </details>;
}
