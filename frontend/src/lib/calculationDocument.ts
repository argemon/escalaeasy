import type { EscalaEasyDocument, ScheduleResult, WorkloadRule } from "../types/schedule";
import { normalizeDocumentTimes } from "./dateTime";

export type GenerationPreferences = { preserveExisting: boolean; balanceUndesirableShifts: boolean };
export const defaultGenerationPreferences: GenerationPreferences = { preserveExisting: true, balanceUndesirableShifts: true };
const alias = (id: string) => `Pessoa ${id}`;
const workload = (rule: WorkloadRule): WorkloadRule => ({ ...rule, weeklyPlan: rule.weeklyPlan ? {
  ...rule.weeklyPlan, compensation: rule.weeklyPlan.compensation ? { ...rule.weeklyPlan.compensation, instrument: "Informado localmente" } : undefined
} : undefined });

// Minimize personal data while preserving all temporal boundaries and identifiers.
// Schedules and functional role names remain personal data; this is not anonymization.
export function calculationDocument(input: EscalaEasyDocument): EscalaEasyDocument {
  const d = normalizeDocumentTimes(input);
  return {
    schemaVersion: d.schemaVersion, id: d.id, updatedAt: d.updatedAt, planningPeriod: { ...d.planningPeriod }, roles: d.roles,
    company: { ...d.company, name: "Empresa", defaultWorkload: workload(d.company.defaultWorkload),
      holidays: d.company.holidays?.map(h => ({ ...h, name: "Feriado" })) },
    employees: d.employees.map(e => ({ ...e, name: alias(e.id), workload: workload(e.workload),
      preferences: e.preferences.map(p => ({ ...p, description: "" })),
      absences: e.absences?.map(a => ({ ...a, category: "unavailable" })) })),
    assignments: d.assignments
  };
}

export function restoreCalculationLabels(result: ScheduleResult, local: EscalaEasyDocument): ScheduleResult {
  const names = new Map(local.employees.map(e => [alias(e.id), e.name]));
  const label = (value: string) => value.replace(/Pessoa [0-9a-f-]{36}/gi, match => names.get(match) ?? match);
  return { ...result,
    conflicts: result.conflicts.map(c => ({ ...c, message: label(c.message) })),
    decisions: result.decisions.map(d => ({ ...d, reason: label(d.reason), factors: d.factors.map(label) })),
    optimizationSuggestions: result.optimizationSuggestions.map(s => ({ ...s, message: label(s.message), impact: label(s.impact) }))
  };
}
