const {test}=require('node:test'),assert=require('node:assert/strict');
const loader=require('./source-loader.cjs');
const load=loader(),{sampleDocument}=load('data/sample.ts');
const {calculationDocument,restoreCalculationLabels}=load('lib/calculationDocument.ts');
const plain=v=>JSON.parse(JSON.stringify(v));
function document(){const d=structuredClone(sampleDocument);d.company.name='Empresa confidencial';const e=d.employees[0];e.name='Nome confidencial';e.absences=[{id:crypto.randomUUID(),category:'medical',start:'2026-11-01T00:00:00',end:'2026-11-02T00:00:00',credits:[{date:'2026-11-01',excusedSeconds:3600}]}];d.publications=[{private:'Arquivo confidencial'}];d.publicationSelections=[];return d;}
test('calculation removes names, absence categories and archives without mutating the original',()=>{
 const d=document(),before=JSON.stringify(d),remote=calculationDocument(d),json=JSON.stringify(remote);
 assert.doesNotMatch(json,/Empresa confidencial|Nome confidencial|medical|Arquivo confidencial|publications|publicationSelections/);
 assert.equal(remote.employees[0].name,'Pessoa '+d.employees[0].id);assert.equal(remote.employees[0].absences[0].category,'unavailable');
 assert.deepEqual(plain(remote.employees[0].absences[0].credits),d.employees[0].absences[0].credits);
 assert.deepEqual(plain(remote.assignments),d.assignments);assert.equal(JSON.stringify(d),before);
});
test('minimized input preserves local analysis and restores labels as literal text',()=>{
 const d=document();delete d.publications;const {analyzeDocument}=load('lib/analysis.ts');
 const local=analyzeDocument(d),remote=restoreCalculationLabels(analyzeDocument(calculationDocument(d)),d);
 assert.deepEqual(plain(remote),plain(local));
 d.employees[0].name='$& $1 Pessoa';const token='Pessoa '+d.employees[0].id;
 const result=restoreCalculationLabels({...local,decisions:[{employeeId:d.employees[0].id,assignmentId:'x',reason:token+' alocado',factors:[token]}]},d);
 assert.equal(result.decisions[0].reason,d.employees[0].name+' alocado');
});
for(const queued of [false,true])test('actual '+(queued?'queued':'direct')+' solve request transmits minimized data',async()=>{
 const d=document();delete d.publications;let body;
 const result=load('lib/analysis.ts').analyzeDocument(d);
 const runtime=loader(undefined,{fetch:async(_url,options)=>{body=JSON.parse(options.body);return {ok:true,status:200,headers:{get:()=>null},json:async()=>({success:true,data:queued?{state:'completed',result}:result})};}});
 if(queued)await runtime('lib/queuedSolve.ts').solveQueued(d,()=>{},{preserveExisting:true,balanceUndesirableShifts:true});
 else await runtime('lib/api.ts').solveSchedule(d);
 assert.doesNotMatch(JSON.stringify(body),/Nome confidencial|Empresa confidencial|medical|publications/);
 assert.equal(body.document.assignments.length,d.assignments.length);
 if(queued)assert.equal(body.preferences.balanceUndesirableShifts,true);
});
