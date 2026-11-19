import { displayTemporalText } from "../lib/displayTime";
import { useDialogFocus } from "../lib/useDialogFocus";
import { useRef, useState } from "react";
import { useScheduleStore } from "../store/scheduleStore";
import { previewShift, type PlanningPreview } from "../lib/planningEdits";
import { employeeOn } from "../lib/policies/employment";
import { shiftOverlapMessage } from "../lib/policies/shiftOverlap";
import { civilMillis, civilString, clockMillis } from "../lib/civilTime";
import type { ShiftAssignment } from "../types/schedule";
import { X } from "lucide-react";
import { Button, IconButton } from "./ui";
export function ShiftEditor(){
 const {document,selectedShift}=useScheduleStore();if(!selectedShift)return null;
 const existing=document.assignments.find(a=>a.id===selectedShift.id),employee=document.employees.find(e=>e.id===selectedShift.employeeId)??document.employees[0];if(!employee)return null;
 const start=selectedShift.date+"T"+document.company.opensAt,effective=employeeOn(employee,start);
 const initial:ShiftAssignment=selectedShift.draft??existing??{id:crypto.randomUUID(),employeeId:employee.id,role:effective.role,roleId:effective.roleId,start,end:civilString(civilMillis(start)+clockMillis((employee.inheritsCompanyDefaults?document.company.defaultWorkload:employee.workload).defaultShiftDuration)),source:"manual",tags:[]};
 return <ShiftForm key={selectedShift.id??selectedShift.employeeId+selectedShift.date} initial={initial} />;
}
function ShiftForm({initial}:{initial:ShiftAssignment}) {
  const {document,analysis,selectShift,applyPlanningPreview}=useScheduleStore();
  const [draft,setDraft]=useState(initial),[preview,setPreview]=useState<PlanningPreview|null>(null);
  const [allow,setAllow]=useState(false),[error,setError]=useState("");
  const existing=document.assignments.some(a=>a.id===draft.id);
  const overlap=shiftOverlapMessage(document,draft);
  const close=()=>selectShift(undefined);
  const ref=useRef<HTMLElement>(null);useDialogFocus(ref,close);
  const patch=(change:Partial<ShiftAssignment>)=>{setDraft({...draft,...change});setPreview(null);setAllow(false);setError("");};
  const run=(fn:()=>void)=>{try{setError("");fn();}catch(e){setError((e as Error).message);}};
  const stamp=(v:string)=>v.length===16?v+":00":v;
  const inputClass="w-full min-w-0 rounded-lg border border-border bg-background px-3 py-2.5 text-base text-foreground";
  const removing=Boolean(preview&&!preview.document.assignments.some(a=>a.id===draft.id));
  const errors=preview?.analysis.conflicts.filter(c=>c.severity==="error")??[];
  const messages=[...new Set(errors.map(c=>{const name=document.employees.find(e=>e.id===c.employeeId)?.name??"Equipe";const day=c.start?.slice(0,10).split("-").reverse().join("/");return name+(day?" · "+day:"")+": "+c.message;}))];
  function save() {
    const checked=previewShift(document,draft);
    if(checked.analysis.conflicts.some(c=>c.severity==="error")){setPreview(checked);setAllow(false);return;}
    applyPlanningPreview(checked,false);
  }
  return <div className="absolute inset-0 z-40 flex justify-end bg-black/40">
    <aside ref={ref} role="dialog" aria-modal="true" aria-label={existing?"Editar turno":"Criar turno"} className="manual-shift-editor flex w-full max-w-lg flex-col overflow-auto bg-card text-foreground">
      <header className="flex items-center justify-between border-b border-border px-5 py-4">
        <h2 className="text-xl font-semibold">{existing?"Editar turno":"Novo turno"}</h2>
        <IconButton type="button" aria-label="Fechar edição do turno" title="Fechar" onClick={close}><X className="size-5" /></IconButton>
      </header>
      <div className="space-y-5 p-5">
        {(overlap||error)&&<p role="alert" className="rounded-lg border border-red-500/40 bg-red-500/10 p-3 text-sm leading-relaxed">{overlap||error}</p>}
        <label className="grid gap-2 font-medium">Pessoa
          <select className={inputClass} value={draft.employeeId} onChange={event=>{const e=employeeOn(document.employees.find(e=>e.id===event.target.value)!,draft.start);patch({employeeId:e.id,role:e.role,roleId:e.roleId});}}>
            {document.employees.map(e=><option key={e.id} value={e.id}>{e.name}</option>)}
          </select>
        </label>
        <fieldset className="space-y-3"><legend className="mb-2 font-semibold">Horário de trabalho</legend>
          {(["start","end"]as const).map(k=><label className="grid gap-2" key={k}>{k==="start"?"Entrada":"Saída"}
            <input className={inputClass} type="datetime-local" step={60} value={draft[k].slice(0,16)} onChange={e=>patch({[k]:stamp(e.target.value)})}/>
          </label>)}
        </fieldset>
        <fieldset className="space-y-3 rounded-xl border border-border p-4"><legend className="px-1 font-semibold">Intervalo</legend>
          <label className="flex items-center gap-2"><input type="checkbox" checked={Boolean(draft.break)} onChange={e=>run(()=>patch({break:e.target.checked?{start:civilString(civilMillis(draft.start)+4*3600000),end:civilString(civilMillis(draft.start)+5*3600000)}:undefined}))}/> Incluir intervalo</label>
          {draft.break&&(["start","end"]as const).map(k=><label className="grid gap-2" key={k}>{k==="start"?"Início do intervalo":"Fim do intervalo"}
            <input className={inputClass} type="datetime-local" step={60} value={draft.break![k].slice(0,16)} onChange={e=>patch({break:{...draft.break!,[k]:stamp(e.target.value)}})}/>
          </label>)}
        </fieldset>
        <details className="rounded-xl border border-border p-4"><summary className="cursor-pointer font-medium">Mais opções</summary>
          <div className="mt-4 space-y-4">
            <label className="grid gap-2">Função<select className={inputClass} value={draft.roleId??""} onChange={e=>{const role=document.roles?.find(r=>r.id===e.target.value);patch({roleId:role?.id,role:role?.name??""});}}>{document.roles?.map(r=><option key={r.id} value={r.id}>{r.name}</option>)}</select></label>
            <label className="flex items-start gap-2"><input type="checkbox" checked={draft.pinned??false} onChange={e=>patch({pinned:e.target.checked})}/> Manter este turno ao gerar a escala</label>
          </div>
        </details>
        {preview&&<section aria-label="Revisão do turno" className="space-y-3 rounded-xl border border-border p-4">
          <h3 className="font-semibold">{removing?"Remover este turno?":"Confira os conflitos da escala"}</h3>
          {removing&&<p className="text-sm">Você poderá usar Desfazer para recuperar o turno.</p>}
          {messages.slice(0,3).map(message=><p className="text-sm leading-relaxed" key={message}>{displayTemporalText(message)}</p>)}
          {messages.length>3&&<details><summary className="cursor-pointer text-sm">Ver os outros {messages.length-3} conflitos</summary><div className="mt-2 space-y-2">{messages.slice(3).map(message=><p className="text-sm" key={message}>{displayTemporalText(message)}</p>)}</div></details>}
          <details><summary className="cursor-pointer text-sm">Ver impacto na escala</summary><p className="mt-2 text-sm">Cobertura: {Math.round(analysis.coverageScore*100)}% → {Math.round(preview.analysis.coverageScore*100)}% · Conflitos: {analysis.conflicts.filter(c=>c.severity==="error").length} → {errors.length}</p></details>
          {errors.length>0&&<label className="flex items-start gap-2 text-sm"><input type="checkbox" checked={allow} onChange={e=>setAllow(e.target.checked)}/> Estou ciente dos conflitos exibidos e quero continuar.</label>}
        </section>}
      </div>
      <footer className="sticky bottom-0 mt-auto flex flex-wrap gap-2 border-t border-border bg-card p-4">
        <Button type="button" disabled={preview?(!removing&&Boolean(overlap))||(errors.length>0&&!allow):Boolean(overlap)} onClick={()=>run(()=>preview?applyPlanningPreview(preview,allow):save())}>{preview?(removing?"Confirmar remoção":"Salvar com os conflitos indicados"):"Salvar turno"}</Button>
        <Button type="button" className="bg-card text-foreground dark:bg-card dark:text-foreground" onClick={close}>Cancelar</Button>
        {existing&&<button type="button" className="ml-auto px-2 text-sm text-red-600 dark:text-red-400" onClick={()=>run(()=>{setAllow(false);setPreview(previewShift(document,undefined,draft.id));})}>Remover turno</button>}
      </footer>
    </aside>
  </div>;
}
