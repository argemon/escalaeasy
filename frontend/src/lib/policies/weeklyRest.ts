import type { CompanyRules, Employee, EscalaEasyDocument, ScheduleIssue, ShiftAssignment } from "../../types/schedule";
import { civilMillis, civilString, civilWeek } from "../civilTime";
import { workloadFor } from "./effectiveWork";
import { activeOn } from "./employment";

export const restDayMillis = 86400000;
export function requiredWeeklyRests(company: CompanyRules, employee: Employee): number {
  const rule = workloadFor(company, employee);
  const days = rule.weeklyPlan?.flexibleWorkDays ?? (rule.regimeType === "dayBased" && rule.workUnits + rule.restUnits === 7 ? rule.workUnits : 0);
  return days > 0 && days < 7 ? 7 - days : 0;
}
export function sundayFrequency(employee: Employee): number {
  return [2,3,4].find(n => employee.preferences.some(p => p.code === `sundayRest${n}` && p.weight > 0)) ?? 0;
}
export function setSundayFrequency(employee: Employee, frequency: number): Employee {
  return {...employee, preferences: [...employee.preferences.filter(p => !/^sundayRest[234]$/.test(p.code)),
    ...([2,3,4].includes(frequency) ? [{code:`sundayRest${frequency}`,weight:1,description:"Rodízio de domingos de descanso"}] : [])]};
}
// Pauses remain part of the shift: lunch never becomes a weekly rest.
export function restWindows(shifts: ShiftAssignment[], start: number, end: number) {
  let cursor = start;
  const gaps: {start:number;end:number}[] = [];
  for (const shift of [...shifts].sort((a,b)=>civilMillis(a.start)-civilMillis(b.start))) {
    const from = Math.max(start,civilMillis(shift.start)), to = Math.min(end,civilMillis(shift.end));
    if (to <= start || from >= end || to <= from) continue;
    if (from > cursor) gaps.push({start:cursor,end:from});
    cursor = Math.max(cursor,to);
  }
  if (cursor < end) gaps.push({start:cursor,end});
  return gaps;
}
export function weeklyRestSummary(d: EscalaEasyDocument, employee: Employee, week: number) {
  const start = civilMillis(d.planningPeriod.start+"T00:00:00"), end = civilMillis(d.planningPeriod.end+"T00:00:00")+restDayMillis;
  const own = d.assignments.filter(a=>a.employeeId===employee.id);
  const full = week >= start && week+7*restDayMillis <= end && activeOn(employee,civilString(week+7*restDayMillis-1));
  const hasSchedule = own.some(a=>civilMillis(a.start)<Math.min(end,week+7*restDayMillis)&&civilMillis(a.end)>Math.max(start,week));
  const windows = restWindows(own,Math.max(start,week),Math.min(end,week+7*restDayMillis));
  return {required:requiredWeeklyRests(d.company,employee), complete:full&&hasSchedule,
    count:windows.reduce((n,w)=>n+Math.floor((w.end-w.start)/restDayMillis),0), windows};
}
export function restDayLabel(d: EscalaEasyDocument, employee: Employee, date: string): string {
  const start = civilMillis(date+"T00:00:00"), end = start+restDayMillis;
  if (!activeOn(employee,date)) return "Pessoa inativa";
  if (employee.absences?.some(a=>civilMillis(a.start)<end&&civilMillis(a.end)>start)) return "Ausência registrada";
  const own = d.assignments.filter(a=>a.employeeId===employee.id);
  if (own.some(a=>civilMillis(a.start)<end&&civilMillis(a.end)>start)) return "Turno iniciado no dia anterior";
  const week = civilWeek(start);
  if (!weeklyRestSummary(d,employee,week).complete) return "Sem turno · descanso não confirmado";
  return "Folga prevista · 24h sem turno";
}
export function weeklyRestIssues(d: EscalaEasyDocument): ScheduleIssue[] {
  const issues: ScheduleIssue[] = [], start = civilMillis(d.planningPeriod.start+"T00:00:00"), end = civilMillis(d.planningPeriod.end+"T00:00:00")+restDayMillis;
  for (const e of d.employees) {
    const own = d.assignments.filter(a=>a.employeeId===e.id);
    const add = (code:string,message:string,from:number,to:number,severity:ScheduleIssue["severity"]="error") => issues.push({code,message,start:civilString(from),end:civilString(to),employeeId:e.id,role:e.role,severity});
    for (let week=civilWeek(start);week<end;week+=7*restDayMillis) {
      const summary=weeklyRestSummary(d,e,week);
      if(summary.complete&&summary.required>0&&summary.count<summary.required)
        add("weeklyRest",`Repouso semanal insuficiente: ${summary.count} de ${summary.required} períodos de 24h sem trabalho na semana. Confira os turnos deste período.`,week,week+7*restDayMillis);
    }
    if(requiredWeeklyRests(d.company,e)>0) for(let day=start;day+7*restDayMillis<=end;day+=restDayMillis) {
      if(!activeOn(e,civilString(day+7*restDayMillis-1)))continue;
      if(restWindows(own,day,day+7*restDayMillis).every(w=>w.end-w.start<restDayMillis))
        add("consecutiveWork","Sete dias sem um período contínuo de 24h de repouso. Confira os turnos deste período.",day,day+7*restDayMillis);
    }
    const frequency=sundayFrequency(e);
    if(frequency)for(let sunday=civilWeek(start);sunday<end;sunday+=7*restDayMillis) {
      if(sunday+restDayMillis<=start)continue;
      const from=sunday-(frequency-1)*7*restDayMillis, to=sunday+restDayMillis;
      if(!activeOn(e,civilString(to-1)))continue;
      const sundays=Array.from({length:frequency},(_,i)=>from+i*7*restDayMillis);
      // A recorded shift proves work outside the period; an empty date there proves nothing.
      if(sundays.every(day=>own.some(a=>civilMillis(a.start)<day+restDayMillis&&civilMillis(a.end)>day)))
        add("sundayRotation",`Rodízio de domingos: nenhum domingo inteiro sem trabalho em ${frequency} semanas, conforme a preferência cadastrada.`,from,to,"warning");
    }
  }
  return issues;
}
