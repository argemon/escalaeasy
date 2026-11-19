import type { CompanyRules, Employee, ShiftAssignment } from "../types/schedule";
import { civilMillis, civilString, clockMillis } from "./civilTime";
import { timePart } from "./dateTime";
import { workMillis } from "./policies/effectiveWork";
import { closuresFor, variableIntervalLimits } from "./policies/interval";

function duration(milliseconds: number): string {
  const seconds = Math.round(milliseconds / 1000);
  const hours = Math.floor(seconds / 3600), minutes = Math.floor(seconds % 3600 / 60), rest = seconds % 60;
  return [hours ? `${hours}h` : "", minutes ? `${minutes}min` : "", rest ? `${rest}s` : ""].filter(Boolean).join(" ");
}

const clock = (value: string) => timePart(value).replace(/:00$/, "");

export function intervalNotice(company: CompanyRules, employee: Employee | undefined, shift: ShiftAssignment) {
  if (!employee || !shift.break) return undefined;
  const start = civilMillis(shift.start), end = civilMillis(shift.end);
  const minimum = variableIntervalLimits(company, employee, workMillis(shift), closuresFor(company, start, end)).minimum;
  const actual = civilMillis(shift.break.end) - civilMillis(shift.break.start);
  if (actual <= minimum) return undefined;
  let reason = employee.fixedInterval ? "Horário fixado no cadastro individual."
    : shift.tags.includes("interval-extension-verified") ? "Ampliação necessária para atender ao horário de atendimento da empresa."
    : "Intervalo salvo acima do mínimo; sua necessidade deve ser reavaliada ao gerar a escala.";
  if (!employee.fixedInterval && employee.shiftStartsAt
    && clockMillis(employee.shiftStartsAt) === clockMillis(timePart(shift.start))
    && shift.tags.includes("interval-extension-verified")) {
    const shorterEnd = civilString(end - (actual - minimum));
    reason = `Intervalo ampliado de ${duration(minimum)} para ${duration(actual)} para atender ao horário de atendimento da empresa até ${clock(shift.end)}. `
      + `Com entrada fixa às ${clock(shift.start)} e jornada de ${duration(workMillis(shift))}, a pausa mínima levaria à saída às ${clock(shorterEnd)}.`;
  }
  return {
    label: `Intervalo maior: ${duration(actual)} (+${duration(actual - minimum)})`,
    shortLabel: `↑ +${duration(actual - minimum)}`,
    description: `${employee.name}: mínimo de ${duration(minimum)}. ${reason}`
  };
}

export function intervalDateRanges(dates: string[]): string {
  const sorted = [...new Set(dates)].sort();
  const ranges: { start: string; end: string }[] = [];
  for (const date of sorted) {
    const previous = ranges[ranges.length - 1];
    if (previous && civilMillis(date + "T00:00:00") - civilMillis(previous.end + "T00:00:00") === 86400000) previous.end = date;
    else ranges.push({ start: date, end: date });
  }
  const format = (date: string) => date.split("-").reverse().join("/");
  return ranges.map(range => `de ${format(range.start)} a ${format(range.end)}`).join("; ");
}

export function intervalNotices(company: CompanyRules, employees: Employee[], shifts: ShiftAssignment[]) {
  const groups = new Map<string, { employeeId: string; label: string; description: string; dates: string[] }>();
  for (const shift of shifts) {
    const notice = intervalNotice(company, employees.find(employee => employee.id === shift.employeeId), shift);
    if (!notice) continue;
    const key = `${shift.employeeId}:${notice.label}:${notice.description}`;
    const group = groups.get(key) ?? { ...notice, employeeId: shift.employeeId, dates: [] };
    const date = shift.start.slice(0, 10);
    if (!group.dates.includes(date)) group.dates.push(date);
    groups.set(key, group);
  }
  return [...groups.values()].map(group => ({ ...group, dates: group.dates.sort() }));
}
