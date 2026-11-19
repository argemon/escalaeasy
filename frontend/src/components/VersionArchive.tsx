import { displayTemporalText } from "../lib/displayTime";
import { displayInstant } from "../lib/displayTime";
import { displayDateTime } from "../lib/displayTime";
import { useMemo, useRef, useState } from "react";
import { useScheduleStore } from "../store/scheduleStore";
import { acknowledgementLabels, closureReview } from "../lib/publications";
import { useDialogFocus } from "../lib/useDialogFocus";
import { Button } from "./ui";

export function VersionArchive({ onClose }: { onClose:()=>void }) {
  const { document, closePublication } = useScheduleStore();
  const ref = useRef<HTMLElement>(null); useDialogFocus(ref,onClose);
  const [acknowledged,setAcknowledged] = useState<string[]>([]), [error,setError] = useState("");
  const review = useMemo(()=>closureReview(document),[document]);
  const [baseline,setBaseline] = useState(document);
  const currentAcknowledged = baseline===document ? acknowledged : [];
  return <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 p-4"><section ref={ref} role="dialog" aria-modal="true" aria-labelledby="version-title" className="max-h-[85vh] w-full max-w-2xl space-y-3 overflow-auto rounded-2xl border border-border bg-card p-5">
    <h2 id="version-title" tabIndex={-1} data-dialog-initial-focus className="text-xl font-semibold">Versões arquivadas</h2>
    <p className="text-sm">Guarde uma referência imutável da escala atual para consultar depois. As versões acompanham o arquivo .escalalivre. O botão Baixar PDF continua usando a escala salva do período atual.</p>
    {review.mandatory.length>0&&<p role="status" className="text-sm">Resolva {review.mandatory.length} conflito(s) obrigatório(s) antes de arquivar uma nova versão.</p>}
    {review.acknowledgements.map(a=><label className="block text-sm" key={a.category}><input type="checkbox" checked={currentAcknowledged.includes(a.category)} onChange={e=>{setBaseline(document);setAcknowledged(e.target.checked?[...currentAcknowledged,a.category]:currentAcknowledged.filter(c=>c!==a.category));}}/> Reconheço: {acknowledgementLabels[a.category]} · {a.occurrences} ocorrência(s)</label>)}
    <Button type="button" disabled={review.mandatory.length>0||review.acknowledgements.some(a=>!currentAcknowledged.includes(a.category))} onClick={()=>{try{closePublication(currentAcknowledged);setAcknowledged([]);setError("");}catch(e){setError((e as Error).message);}}}>Arquivar escala atual</Button>
    {error&&<p role="alert">{displayTemporalText(error)}</p>}
    {!document.publications?.length&&<p className="text-sm">Nenhuma versão arquivada.</p>}
    {[...(document.publications??[])].reverse().map(v=><details key={v.id} className="rounded border border-border p-3"><summary className="cursor-pointer text-sm">Versão {v.number} · {v.period.start.split("-").reverse().join("/")} a {v.period.end.split("-").reverse().join("/")} · {v.shifts.length} turnos</summary>
      <p className="my-2 text-xs">Arquivada em {displayInstant(v.closedAt)}. Consulta somente; não substitui o planejamento atual.</p>
      <div className="overflow-x-auto"><table className="w-full text-left text-xs"><thead><tr><th>Pessoa</th><th>Função</th><th>Entrada</th><th>Saída</th><th>Intervalo</th></tr></thead><tbody>{v.shifts.map(s=><tr className="border-t border-border" key={s.id}><th scope="row" className="py-2">{v.people.find(p=>p.id===s.employeeId)?.name}</th><td>{s.role}</td><td>{displayDateTime(s.start)}</td><td>{displayDateTime(s.end)}</td><td>{s.break?`${displayDateTime(s.break.start)} – ${displayDateTime(s.break.end)}`:"Sem intervalo"}</td></tr>)}</tbody></table></div>
    </details>)}
    <p className="text-xs text-muted-foreground">São preservadas até 10 versões, sujeitas ao espaço disponível. Exporte seu arquivo para manter uma cópia fora deste navegador.</p>
    <Button type="button" onClick={onClose}>Fechar versões</Button>
  </section></div>;
}
