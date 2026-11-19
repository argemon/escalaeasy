const {test}=require('node:test');
const assert=require('node:assert/strict');
const fs=require('node:fs'),path=require('node:path'),vm=require('node:vm'),ts=require('typescript');
function setup(outcome="success",realStore){
 let complete,applied=0,calls=0;const original={id:'original',planningPeriod:{start:'2026-11-04',end:'2026-11-04'}};
 const schedule={document:original,setSolverStatus(s){schedule.solverStatus=s;},applyGeneratedSchedule(){applied++;if(outcome==="error")throw Error("Resposta inválida");if(outcome==="invalid"){schedule.solverStatus="invalid";return;}if(outcome==="storage"){schedule.solverStatus="idle";return;}schedule.document={...schedule.document};}};
 const exports={};
 const source=fs.readFileSync(path.join(__dirname,'../src/store/generationStore.ts'),'utf8');
 const code=ts.transpileModule(source,{compilerOptions:{module:ts.ModuleKind.CommonJS,target:ts.ScriptTarget.ES2022}}).outputText;
 vm.runInNewContext(code,{exports,Date,Error,require:r=>r==='../lib/scheduleQuality'?{changedPeople:(before,after)=>before.assignments&&after.assignments?require('./source-loader.cjs')()('lib/scheduleQuality.ts').changedPeople(before,after):0}:r==='zustand'?require(r):r==='./scheduleStore'?{useScheduleStore:{getState:()=>realStore?.getState()??schedule}}:r==='../lib/planningPeriod'?{periodError:()=>null}:r==='../lib/generationProposal'?{generationProposal:()=>({})}:{
  solveQueued:async(_document,progress)=>{calls++;progress({id:'token',state:'queued'});return new Promise(resolve=>{complete=resolve;});},
  cancelQueuedSolve:async()=>{}
 }});
 return {store:exports.useGenerationStore,schedule,complete:r=>complete(r),applied:()=>applied,calls:()=>calls};
}
test('a shared active generation prevents duplicate submissions across remounted panels',async()=>{
 const s=setup();const pending=s.store.getState().start();await s.store.getState().start();
 assert.equal(s.calls(),1);assert.equal(s.store.getState().active,true);
 s.complete({assignments:[]});await pending;assert.equal(s.applied(),1);assert.match(s.store.getState().message,/gerada e aplicada/);assert.equal(s.store.getState().active,false);
});
test('edits made while waiting are never overwritten by an older result',async()=>{
 const s=setup();const pending=s.store.getState().start();s.schedule.document={id:'changed'};
 s.complete({assignments:[]});await pending;
 assert.equal(s.applied(),0);assert.match(s.store.getState().message,/planejamento mudou/);
});
test('cancelled generation keeps existing shifts and resets the generation status',async()=>{
 const s=setup();const pending=s.store.getState().start();await s.store.getState().cancel();s.complete(null);await pending;
 assert.equal(s.applied(),0);assert.equal(s.schedule.solverStatus,'idle');assert.match(s.store.getState().message,/cancelada/);
});

test('generation applies immediately and supports a subsequent generation',async()=>{const s=setup();for(let i=1;i<=2;i++){const pending=s.store.getState().start();s.complete({assignments:[]});await pending;assert.equal(s.applied(),i);assert.match(s.store.getState().message,/desfazer/);}});
for(const outcome of ['invalid','storage','error'])test('failed application preserves the document: '+outcome,async()=>{const s=setup(outcome),before=s.schedule.document,pending=s.store.getState().start();s.complete({assignments:[]});await pending;assert.equal(s.schedule.document,before);assert.equal(s.store.getState().active,false);assert.doesNotMatch(s.store.getState().message,/gerada e aplicada/);assert.match(s.store.getState().message,outcome==='storage'?/recuperável/:outcome==='invalid'?/mantidos/:/inválida/);});

test('one generation updates the real schedule and Desfazer restores all prior shifts',async()=>{
 const load=require('./source-loader.cjs')(),real=load('store/scheduleStore.ts').useScheduleStore;
 const d=structuredClone(require('../../shared/pinned-generation-scenarios.json').scenarios[0].request.document);
 real.getState().importDocument(d);const before=JSON.stringify(real.getState().document);
 const result={...real.getState().analysis,optimization:{status:'feasible'},assignments:structuredClone(real.getState().document.assignments)};
 const s=setup('success',real),pending=s.store.getState().start();s.complete(result);await pending;
 assert.match(s.store.getState().message,/gerada e aplicada/);assert.equal(real.getState().undoCount,1);
 real.getState().undo();assert.equal(JSON.stringify(real.getState().document),before);
 real.getState().redo();assert.equal(real.getState().undoCount,1);
});
