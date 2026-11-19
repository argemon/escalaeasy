import type { CompanyRules, Employee, EscalaLivreDocument, ShiftAssignment } from "../types/schedule";
import { civilDay, civilMillis, civilString, endOn } from "./civilTime";
import { isNight } from "./policies/eligibility";
import { employeeOn, activeOn } from "./policies/employment";
import { workloadFor } from "./policies/effectiveWork";
import { closedHoliday } from "./holidays";

export const qualityCategories = { sunday: "Domingos", holiday: "Feriados", closing: "Fechamentos", night: "Noturnos" };
export type QualityCategory = keyof typeof qualityCategories;
export const qualityHistoryDays = 28;
export function countsForQuality(company: CompanyRules, shift: ShiftAssignment, category: QualityCategory): boolean {
  const start = civilMillis(shift.start), end = civilMillis(shift.end);
  switch (category) {
    case "sunday": return new Date(start).getUTCDay() === 0;
    case "holiday": return Boolean(company.holidays?.some(h => h.date === shift.start.slice(0,10))) || closedHoliday(company,shift.start.slice(0,10));
    case "closing": return end === endOn(civilDay(start),company.opensAt,company.closesAt);
    case "night": return isNight(start,end);
  }
}
export function qualityComparisonGroup(company: CompanyRules, person: Employee, on: string): string {
  const e = employeeOn(person,on), rule = workloadFor(company,e);
  const parts = rule.defaultShiftDuration.split(":").map(Number);
  const duration = parts[0]*3600+parts[1]*60+(parts[2]??0);
  return `${e.roleId??e.role}|${rule.weeklyPlan?.contractedSeconds??duration}|${rule.weeklyPlan ? "True" : "False"}`;
}
export function qualitySummary(d: EscalaLivreDocument) {
  const from = d.planningPeriod.start, through = d.planningPeriod.end;
  const historyStart = civilString(civilMillis(from+"T00:00:00")-qualityHistoryDays*86400000).slice(0,10);
  return { historyStart, people: d.employees.filter(e => activeOn(e,from)).map(e => {
    const rule = workloadFor(d.company,e);
    const reference = rule.weeklyPlan ? `${(rule.weeklyPlan.contractedSeconds/3600).toLocaleString("pt-BR")} h/semana` : `${rule.defaultShiftDuration} por turno`;
    const own = d.assignments.filter(a => a.employeeId === e.id);
    const counts = (history: boolean) => Object.fromEntries((Object.keys(qualityCategories) as QualityCategory[]).map(category => [category,
      own.filter(a => { const date=a.start.slice(0,10); return (history ? date>=historyStart&&date<from : date>=from&&date<=through)&&countsForQuality(d.company,a,category); }).length
    ])) as Record<QualityCategory,number>;
    return { id:e.id, name:e.name, role:employeeOn(e,from).role, reference, group:qualityComparisonGroup(d.company,e,from), current:counts(false), history:counts(true) };
  }) };
}
export function changedPeople(before: EscalaLivreDocument, after: EscalaLivreDocument): number {
  const signature = (a:ShiftAssignment) => JSON.stringify([a.employeeId,a.roleId??null,a.role,a.start,a.end,a.break?.start??null,a.break?.end??null]);
  const own = (d:EscalaLivreDocument,id:string) => d.assignments.filter(a=>a.employeeId===id).map(signature).sort().join("|");
  return before.employees.filter(e=>own(before,e.id)!==own(after,e.id)).length;
}
