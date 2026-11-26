import type { EscalaLivreDocument } from "../types/schedule";
import { exportQueued, type ExportProgress } from "./queuedExport";
import { publicDocument, downloadBlob } from "./publicationExport";
import { nativeFilename } from "./nativeDocument";

// Print only saved shifts in the current period, with no archive or private staff data.
export function schedulePdfDocument(d: EscalaLivreDocument) {
  return publicDocument({
    id: d.id, number: 0, closedAt: d.updatedAt, period: { ...d.planningPeriod },
    companyName: d.company.name, companyTimeZone: d.company.timeZone ?? "America/Cuiaba",
    people: d.employees.map(e => ({ id: e.id, name: e.name, role: e.role })),
    shifts: d.assignments.filter(s => s.start.slice(0, 10) >= d.planningPeriod.start && s.start.slice(0, 10) <= d.planningPeriod.end)
      .map(s => ({ id: s.id, employeeId: s.employeeId, role: s.role, start: s.start, end: s.end,
        ...(s.break ? { break: { ...s.break } } : {}) })),
    acknowledgements: []
  });
}

export async function exportSchedulePdf(d: EscalaLivreDocument,onProgress?:(p:ExportProgress)=>void) {
  const document = schedulePdfDocument(d);
  if (!document.assignments.length) throw Error("Não há turnos salvos neste período para imprimir.");
  const result=await exportQueued({document,format:"pdf"},onProgress);
  if(!result)return false;
  const bytes = Uint8Array.from(atob(result.contentBase64), c => c.charCodeAt(0));
  downloadBlob(new Blob([bytes], { type: "application/pdf" }), nativeFilename(d).replace(/\.(?:escalaeasy|escalalivre)$/, ".pdf"));
  return true;
}
