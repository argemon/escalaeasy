import { useState } from "react";
import { X } from "lucide-react";
import { intervalDateRanges, intervalNotices } from "../lib/intervalNotice";
import { useScheduleStore } from "../store/scheduleStore";

export function IntervalNotices() {
  const document = useScheduleStore(state => state.document);
  const [dismissed, setDismissed] = useState("");
  const notices = intervalNotices(document.company, document.employees, document.assignments.filter(shift =>
    shift.start.slice(0, 10) >= document.planningPeriod.start && shift.start.slice(0, 10) <= document.planningPeriod.end));
  const signature = JSON.stringify([document.id, document.planningPeriod, notices]);
  if (!notices.length || dismissed === signature) return null;
  return <section aria-label="Avisos sobre intervalos" role="status" className="relative mx-3 mt-2 max-h-24 shrink-0 overflow-auto rounded-md border border-amber-500/40 bg-amber-500/10 py-2 pl-3 pr-10 text-xs">
    <button type="button" aria-label="Fechar aviso de intervalos" title="Fechar aviso" onClick={() => setDismissed(signature)} className="absolute right-2 top-2 rounded p-1 hover:bg-amber-500/20 focus-visible:outline focus-visible:outline-2"><X className="size-4" /></button>
    <ul className="space-y-1">
      {notices.map(notice => <li key={notice.employeeId + notice.label + notice.description}>
        <p><strong>{notice.label} · {intervalDateRanges(notice.dates)}.</strong> {notice.description}</p>
      </li>)}
    </ul>
  </section>;
}
