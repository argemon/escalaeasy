const {test}=require('node:test'),assert=require('node:assert/strict');
const loader=require('./source-loader.cjs'),fixtures=require('../../shared/shift-overlap-scenarios.json');
const load=loader(),{introducedShiftOverlap}=load('lib/policies/shiftOverlap.ts');
for(const s of fixtures.scenarios)test('shared edit overlap: '+s.id,()=>{
 const shifts=values=>values.map(a=>({...a,employeeId:a.employee}));
 assert.equal(Boolean(introducedShiftOverlap(shifts(s.before),shifts(s.after))),s.blocked);
});
test('preview, confirmation with acknowledgement, and direct saves reject duplicates atomically',()=>{
 const local=loader(),{sampleDocument}=local('data/sample.ts'),{previewShift,confirmPlanning}=local('lib/planningEdits.ts');
 const {useScheduleStore:store}=local('store/scheduleStore.ts');
 const d=structuredClone(sampleDocument),a=d.assignments[0];d.assignments=[a];store.getState().importDocument(d);
 const before=store.getState().document,duplicate={...a,id:crypto.randomUUID()};
 assert.throws(()=>previewShift(before,duplicate),/já possui um turno/);
 const forged={baseline:JSON.stringify(before),document:{...before,assignments:[a,duplicate]},label:'Salvar',analysis:store.getState().analysis};
 assert.throws(()=>confirmPlanning(before,forged,true),/já possui um turno/);
 assert.throws(()=>store.getState().upsertAssignment(duplicate),/já possui um turno/);
 assert.equal(store.getState().document,before);assert.equal(store.getState().undoCount,0);
 // Existing duplicates remain importable; removal repairs them without deleting history silently.
 assert.equal(store.getState().importDocument(forged.document),true);
 const p=previewShift(store.getState().document,undefined,duplicate.id);
 store.getState().applyPlanningPreview(p,true);assert.equal(store.getState().document.assignments.length,1);
});
test('moving a saved shift cannot bypass overlap checks',()=>{
 const local=loader(),{sampleDocument}=local('data/sample.ts'),{useScheduleStore:store}=local('store/scheduleStore.ts');
 const d=structuredClone(sampleDocument),a=d.assignments[0];
 const later={...a,id:crypto.randomUUID(),start:'2026-05-20T09:00:00',end:'2026-05-20T17:00:00',break:{start:'2026-05-20T12:00:00',end:'2026-05-20T13:00:00'}};
 d.assignments=[a,later];store.getState().importDocument(d);const before=store.getState().document;
 assert.throws(()=>store.getState().moveAssignment(later.id,a.start,a.end),/já possui um turno/);
 assert.equal(store.getState().document,before);
});
