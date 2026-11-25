import { useEffect,useState } from "react";
import { useExportStore } from "../store/exportStore";
import { durationLabel } from "../lib/queuedSolve";
import { Button } from "./ui";

export function ExportProgress(){
  const {active,cancelling,progress,updatedAt,message,failed,cancel,dismiss}=useExportStore();
  const [now,setNow]=useState(Date.now());
  useEffect(()=>{if(!active)return;const timer=window.setInterval(()=>setNow(Date.now()),1000);return()=>window.clearInterval(timer);},[active]);
  const extra=Math.max(0,Math.floor((now-updatedAt)/1000));
  if(!active&&!message)return null;
  return <section aria-label="Andamento da exportação" className="border-t border-primary/30 bg-primary/5 px-4 py-3 text-sm">
    {active&&<>
      <p role="status" aria-live="polite" className="font-semibold">{cancelling?"Cancelando exportação…":progress?.reconnecting?"Reconectando à exportação…":progress?.state==="queued"?`Exportação na ${progress.position}ª posição da fila`:progress?"Preparando seu PDF…":"Entrando na fila de exportação…"}</p>
      {progress&&<p className="mt-1 text-xs">{progress.state==="queued"?`${progress.jobsAhead} pedido(s) à frente. Espera estimada: cerca de ${durationLabel(progress.estimatedWaitSeconds)}.`:`Processamento estimado: cerca de ${durationLabel(progress.estimatedRunSeconds)}.`} Tempo decorrido: {durationLabel(progress.elapsedSeconds+extra)}.</p>}
      {progress?.reconnecting&&<p className="text-xs">Mostramos a última posição confirmada. A reconexão não envia outro pedido.</p>}
      <div className="mt-2 flex flex-wrap items-center gap-3"><p className="text-xs">Mantenha esta página aberta. Você pode continuar usando o app enquanto aguarda.</p><Button type="button" className="h-8 bg-card text-foreground dark:bg-card dark:text-foreground" disabled={!progress||cancelling} onClick={()=>void cancel()}>{cancelling?"Aguardando cancelamento…":"Cancelar exportação"}</Button></div>
    </>}
    {message&&<div className="flex items-center justify-between gap-3"><p role={failed?"alert":"status"}>{message}</p>{!active&&<Button type="button" className="h-8 shrink-0" onClick={dismiss}>Fechar aviso</Button>}</div>}
  </section>;
}
