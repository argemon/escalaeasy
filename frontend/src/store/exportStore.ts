import { create } from "zustand";
import { cancelQueuedExport, type ExportProgress } from "../lib/queuedExport";
import { exportSchedulePdf } from "../lib/schedulePdf";
import type { EscalaLivreDocument } from "../types/schedule";

type ExportState={active:boolean;cancelling:boolean;progress?:ExportProgress;updatedAt:number;message?:string;failed:boolean;
  startPdf:(document:EscalaLivreDocument)=>Promise<void>;cancel:()=>Promise<void>;dismiss:()=>void};
export const useExportStore=create<ExportState>((set,get)=>({
  active:false,cancelling:false,updatedAt:0,failed:false,
  startPdf:async document=>{
    if(get().active)return;
    // Freeze the submitted snapshot even if the user edits while waiting.
    const submitted=structuredClone(document);
    set({active:true,cancelling:false,progress:undefined,message:undefined,failed:false,updatedAt:Date.now()});
    try{
      const downloaded=await exportSchedulePdf(submitted,progress=>set(progress.reconnecting?{progress}:{progress,updatedAt:Date.now()}));
      set({message:downloaded?"PDF preparado. O download foi iniciado com a escala salva no momento do pedido.":"Exportação cancelada. Seu planejamento foi mantido."});
    }catch(error){set({failed:true,message:error instanceof Error?error.message:"Não foi possível exportar o PDF."});}
    finally{set({active:false,cancelling:false,progress:undefined});}
  },
  cancel:async()=>{
    const id=get().progress?.id;
    if(!get().active||!id||get().cancelling)return;
    set({cancelling:true,message:undefined});
    try{await cancelQueuedExport(id);}
    catch{if(get().active)set({cancelling:false,message:"Não foi possível confirmar o cancelamento. Continuamos acompanhando seu pedido; tente novamente."});}
  },
  dismiss:()=>set({message:undefined,failed:false})
}));
