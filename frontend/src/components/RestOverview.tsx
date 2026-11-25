import { useMemo } from "react";
import { civilMillis, civilString, civilWeek } from "../lib/civilTime";
import { requiredWeeklyRests, sundayFrequency, weeklyRestSummary, restDayMillis } from "../lib/policies/weeklyRest";
import { useScheduleStore } from "../store/scheduleStore";

export function RestOverview() {
  const {document,setSelectedEmployeeId}=useScheduleStore();
  const rows=useMemo(()=>document.employees.flatMap(employee=>{
    const result=[];
    for(let week=civilWeek(civilMillis(document.planningPeriod.start+"T00:00:00"));week<=civilMillis(document.planningPeriod.end+"T00:00:00");week+=7*restDayMillis){
      const summary=weeklyRestSummary(document,employee,week);
      result.push({employee,week,summary});
    }
    return result;
  }),[document]);
  return <details className="shrink-0 border-b border-border px-4 py-2 text-xs">
    <summary className="cursor-pointer font-semibold">Folgas e repousos da equipe</summary>
    <p className="my-2 text-muted-foreground">Conferência dos turnos salvos, incluindo os noturnos. Cada repouso tem 24h contínuas. Semana parcial ou sem escala: descanso não confirmado. Ausência registrada é apresentada separadamente das folgas. Os indicadores seguem o ciclo cadastrado.</p>
    <div className="max-h-64 overflow-auto"><table className="w-full text-left"><thead><tr><th>Pessoa</th><th>Semana a partir de</th><th>Repousos de 24h</th><th>Domingos</th></tr></thead><tbody>{rows.map(({employee,week,summary})=><tr key={employee.id+week} className="border-t border-border"><td className="py-2"><button className="underline" onClick={()=>setSelectedEmployeeId(employee.id)}>{employee.name}</button></td><td>{civilString(week).slice(0,10).split("-").reverse().join("/")}</td><td>{!summary.complete?"Não confirmado · dados parciais ou sem escala":`${summary.count} encontrados${requiredWeeklyRests(document.company,employee)?` / ${summary.required} previstos`:" · ciclo sem meta semanal"}`}</td><td>{sundayFrequency(employee)?`Preferência: 1 a cada ${sundayFrequency(employee)} semanas`:"Rodízio não configurado"}</td></tr>)}</tbody></table></div>
  </details>;
}
