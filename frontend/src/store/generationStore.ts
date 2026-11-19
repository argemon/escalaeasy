import { create } from "zustand";
import { cancelQueuedSolve, solveQueued, type JobProgress } from "../lib/queuedSolve";
import { periodError, type PlanningPeriod } from "../lib/planningPeriod";
import { useScheduleStore } from "./scheduleStore";
import type { GenerationPreferences } from "../lib/calculationDocument";
import { changedPeople } from "../lib/scheduleQuality";
type GenerationState = {active:boolean;cancelling:boolean;progress?:JobProgress;updatedAt:number;message?:string;start:(range?:PlanningPeriod,preferences?:GenerationPreferences)=>Promise<void>;cancel:()=>Promise<void>};
export const useGenerationStore=create<GenerationState>((set,get)=>({
 active:false,cancelling:false,updatedAt:0,
 start:async (selectedRange,preferences)=>{
  if(get().active)return;
  const submitted=useScheduleStore.getState().document,range=selectedRange??submitted.planningPeriod;
  set({active:true,cancelling:false,progress:undefined,message:undefined,updatedAt:Date.now()});useScheduleStore.getState().setSolverStatus("calculating");
  try{
   const invalid=periodError(range);if(invalid||range.start<submitted.planningPeriod.start||range.end>submitted.planningPeriod.end)throw new Error(invalid??"O recorte precisa estar dentro do período selecionado.");
   const calculation={...submitted,planningPeriod:range};
   const result=await solveQueued(calculation,progress=>set(progress.reconnecting?{progress}:{progress,updatedAt:Date.now()}),preferences);
   const current=useScheduleStore.getState();
   if(!result||current.document!==submitted){if(current.document===submitted)current.setSolverStatus("idle");set({message:result?"O planejamento mudou durante a espera. Gere novamente para usar os dados atuais.":"Geração cancelada. Seus turnos foram mantidos."});return;}
   current.applyGeneratedSchedule(submitted,result,range);
   const applied=useScheduleStore.getState();
   set({message:applied.document!==submitted
    ?`Escala gerada e aplicada. ${changedPeople(submitted,applied.document)} pessoa(s) tiveram turnos alterados. Você pode desfazer a operação.`
    :applied.solverStatus==="invalid"
     ?"Não foi possível gerar a escala. Seus turnos foram mantidos. Confira os pontos de atenção."
     :"Não foi possível guardar a cópia recuperável. Seus turnos foram mantidos. Resolva a recuperação e gere novamente."});
  }catch(e){if(useScheduleStore.getState().document===submitted)useScheduleStore.getState().setSolverStatus("invalid");set({message:e instanceof Error?e.message:"Não foi possível gerar a escala."});}
  finally{set({active:false,cancelling:false});}
 },
 cancel:async()=>{const id=get().progress?.id;if(!id||!get().active||get().cancelling)return;set({cancelling:true,message:undefined});try{await cancelQueuedSolve(id);}catch{if(get().active)set({cancelling:false,message:"Não foi possível confirmar o cancelamento. Estamos acompanhando seu pedido; tente cancelar novamente."});}}
}));
