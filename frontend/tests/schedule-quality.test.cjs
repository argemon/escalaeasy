const {test}=require('node:test'),assert=require('node:assert/strict');
const load=require('./source-loader.cjs')();
const {countsForQuality,qualityCategories,qualitySummary,changedPeople}=load('lib/scheduleQuality.ts');
const {sampleDocument}=load('data/sample.ts');
const fixture=require('../../shared/schedule-quality-scenarios.json');
for(const s of fixture.scenarios)test('shared quality classification: '+s.id,()=>{
 const company={...sampleDocument.company,opensAt:s.opensAt,closesAt:s.closesAt,holidays:s.holiday?[{date:s.holiday,name:'Feriado',closed:false}]:[]};
 const actual=Object.keys(qualityCategories).filter(c=>countsForQuality(company,s,c));
 assert.deepEqual(actual,s.expected);
});
test('quality history excludes future shifts and includes exactly the prior 28 days',()=>{
 const d=structuredClone(sampleDocument),e=d.employees[0];d.employees=[e];d.planningPeriod={start:'2026-11-15',end:'2026-11-15'};
 const shift=date=>({id:crypto.randomUUID(),employeeId:e.id,role:e.role,start:date+'T10:00:00',end:date+'T18:00:00',source:'manual',tags:[]});
 d.assignments=['2026-10-11','2026-10-18','2026-11-08','2026-11-15','2026-11-22'].map(shift);
 const summary=qualitySummary(d);assert.equal(summary.historyStart,'2026-10-18');assert.equal(summary.people[0].history.sunday,2);assert.equal(summary.people[0].current.sunday,1);
 const next=structuredClone(d);next.assignments=next.assignments.map(a=>({...a,id:crypto.randomUUID(),source:'solver',tags:['x']}));
 assert.equal(changedPeople(d,next),0);next.assignments[0].end='2026-10-11T17:00:00';assert.equal(changedPeople(d,next),1);
});
