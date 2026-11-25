import { displayDateTime, displayDuration } from "../lib/displayTime";
import { civilMillis } from "../lib/civilTime";
import { restMillis } from "../lib/policies/rest";
import type { OperationalItem } from "../lib/operationalSummary";
import { useScheduleStore } from "../store/scheduleStore";

export function RestIssueDetails({item}:{item:OperationalItem}) {
  const {document,selectShift}=useScheduleStore();
  if(!["weeklyRest","consecutiveWork","sundayRotation","minimumRest"].includes(item.code)||!item.employeeId||!item.start||!item.end)return null;
  const own=document.assignments.filter(a=>a.employeeId===item.employeeId).sort((a,b)=>a.start.localeCompare(b.start));
  const involved=own.filter(a=>a.start<item.end!&&a.end>item.start!);
  const previous=own.filter(a=>a.start<item.start!).sort((a,b)=>b.end.localeCompare(a.end))[0];
  if(item.code==="minimumRest"&&previous&&!involved.some(a=>a.id===previous.id))involved.unshift(previous);
  const actual=previous?(civilMillis(item.start)-civilMillis(previous.end))/1000:undefined;
  const employee=document.employees.find(e=>e.id===item.employeeId);
  return <details className="my-2 text-xs"><summary className="cursor-pointer underline">Ver turnos envolvidos</summary>
    {item.code==="minimumRest"&&actual!==undefined&&employee&&<p>{actual<0?"Há sobreposição de turnos.":`Descanso entre os turnos: ${displayDuration(actual)}.`} Mínimo aplicável: {displayDuration(restMillis(document.company,employee)/1000)}.</p>}
    {involved.map(a=><button key={a.id} className="my-1 block text-left underline" onClick={()=>selectShift({id:a.id,employeeId:a.employeeId,date:a.start.slice(0,10)})}>{displayDateTime(a.start)} até {displayDateTime(a.end)}</button>)}
  </details>;
}
