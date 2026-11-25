import type { EscalaLivreDocument } from "../types/schedule";
import type { JobProgress } from "./queuedSolve";

const API_URL=(import.meta.env.VITE_API_URL??(import.meta.env.DEV?"http://localhost:5003":"")).replace(/\/$/,"");
export type ExportResult={fileName:string;contentBase64:string;mimeType:string};
export type ExportProgress=Omit<JobProgress,"result">&{result?:ExportResult};
export type ExportRequest={document:EscalaLivreDocument;format:"pdf"|"xlsx";publicationLabel?:string};
class ExportResponseError extends Error {
  constructor(message:string,public status:number){super(message);}
}
async function request(path:string,options?:{method?:string;headers?:Record<string,string>;body?:string}):Promise<ExportProgress>{
  const response=await fetch(API_URL+path,{...options,signal:AbortSignal.timeout(15000)});
  const envelope=await response.json().catch(()=>null);
  if(!response.ok||!envelope?.success||!envelope.data)
    throw new ExportResponseError(envelope?.errors?.map((e:{message:string})=>e.message).join(" ")
      ||(response.status===429?"Muitas solicitações. Aguarde antes de tentar novamente.":"Não foi possível acessar a fila de exportação."),response.status);
  return envelope.data;
}
export function cancelQueuedExport(id:string){return request("/export/jobs/"+encodeURIComponent(id),{method:"DELETE"});}

export async function exportQueued(input:ExportRequest,onProgress:(p:ExportProgress)=>void=()=>{}):Promise<ExportResult|null>{
  const body=JSON.stringify(input);
  if(new TextEncoder().encode(body).byteLength>2*1024*1024)throw Error("O pedido de exportação excede o limite de 2 MB.");
  let progress:ExportProgress;
  try{progress=await request("/export/jobs",{method:"POST",headers:{"Content-Type":"application/json"},body});}
  catch(error){
    if(error instanceof ExportResponseError)throw error;
    throw Error("Não foi possível confirmar a entrada na fila de exportação. Aguarde antes de tentar novamente.");
  }
  for(;;){
    onProgress(progress);
    if(progress.state==="completed"){
      if(!progress.result)throw Error("O servidor não retornou o arquivo exportado.");
      if(progress.result.contentBase64.length>4*Math.ceil(10*1024*1024/3))throw Error("O arquivo exportado excede o limite de 10 MB.");
      return progress.result;
    }
    if(progress.state==="cancelled")return null;
    if(progress.state==="failed")throw Error(progress.error||"Não foi possível concluir a exportação.");
    await new Promise(resolve=>setTimeout(resolve,Math.max(3,progress.pollAfterSeconds)*1000));
    try{progress=await request("/export/jobs/"+encodeURIComponent(progress.id));}
    catch(error){
      if(error instanceof ExportResponseError&&[400,401,403,404,410].includes(error.status))throw error;
      // A reconnect never loses the ticket or submits another export.
      progress={...progress,reconnecting:true};
    }
  }
}
