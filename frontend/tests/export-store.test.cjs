const {test}=require('node:test'),assert=require('node:assert/strict');
const loader=require('./source-loader.cjs');
function fixture(){const d=structuredClone(require('../../shared/simultaneous-coverage-scenarios.json').scenarios[0].request.document);d.planningPeriod={start:'2026-11-04',end:'2026-11-06'};const e=d.employees[0];d.assignments=[{id:crypto.randomUUID(),employeeId:e.id,role:e.role,start:'2026-11-04T08:00:00',end:'2026-11-04T17:00:00',source:'manual',tags:[]}];return d;}
function runtime(fetch,timers=[]){
 const files=[];
 return {files,env:{fetch,Blob,atob,Uint8Array,setTimeout:fn=>{timers.push(fn);return 1;},
  URL:{createObjectURL:()=> 'blob:test',revokeObjectURL(){}},
  document:{createElement:()=>({set download(v){files.push(v);},set href(v){},click(){}})}
 }};
}
test('one export is submitted and editing while queued preserves the original snapshot and filename',async()=>{
 const d=fixture();d.company.name='Empresa original';const original=d.assignments[0].start;let release,posted=0,payload;
 const r=runtime(async(_,o)=>{posted++;payload=JSON.parse(o.body);return new Promise(resolve=>{release=resolve;});});
 const store=loader(undefined,r.env)('store/exportStore.ts').useExportStore;
 const pending=store.getState().startPdf(d);assert.equal(store.getState().active,true);
 d.company.name='Empresa editada';d.assignments[0].start='2026-11-05T09:00:00';await store.getState().startPdf(d);
 release(Response.json({success:true,data:{id:'ticket',state:'completed',result:{contentBase64:'JVBERi0=',mimeType:'application/pdf'}}}));
 await pending;
 assert.equal(posted,1);assert.equal(payload.document.company.name,'Empresa original');assert.equal(payload.document.assignments[0].start,original);
 assert.equal(r.files[0],'Empresa original - 04-11-2026 a 06-11-2026.pdf');assert.equal(store.getState().active,false);assert.equal(store.getState().failed,false);
});
test('cancelling an export downloads nothing and never mutates the planning document',async()=>{
 const d=fixture(),before=JSON.stringify(d),timers=[];const job={id:'secret',state:'queued',position:2,jobsAhead:1,pollAfterSeconds:3};
 const paths=[];let cancelled=false;
 const r=runtime(async(path,o)=>{paths.push([path,o.method]);if(o.method==='DELETE')cancelled=true;return Response.json({success:true,data:{...job,state:cancelled?'cancelled':'queued'}});},timers);
 const store=loader(undefined,r.env)('store/exportStore.ts').useExportStore;
 const pending=store.getState().startPdf(d);
 for(let i=0;i<10&&!store.getState().progress;i++)await new Promise(resolve=>setImmediate(resolve));
 await store.getState().cancel();assert.equal(cancelled,true);timers.shift()();await pending;
 assert.equal(r.files.length,0);assert.equal(JSON.stringify(d),before);assert.match(store.getState().message,/cancelada/);
 assert.equal(paths.filter(p=>p[1]==='POST').length,1);
});
