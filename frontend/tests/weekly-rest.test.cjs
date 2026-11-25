const {test}=require('node:test'),assert=require('node:assert/strict');
const load=require('./source-loader.cjs')();
const {restWindows,restDayMillis,weeklyRestSummary,weeklyRestIssues,restDayLabel,sundayFrequency,setSundayFrequency}=load('lib/policies/weeklyRest.ts');
const {civilMillis,civilWeek}=load('lib/civilTime.ts');
const {sampleDocument}=load('data/sample.ts');
const scenarios=require('../../shared/weekly-rest-scenarios.json');
for(const s of scenarios.scenarios)test('shared continuous rest: '+s.id,()=>{
 const shifts=s.shifts.map(([start,end])=>({start,end}));
 const windows=restWindows(shifts,civilMillis(s.start),civilMillis(s.end));
 assert.equal(windows.reduce((n,w)=>n+Math.floor((w.end-w.start)/restDayMillis),0),s.count);
});
function fixture(){const d=structuredClone(sampleDocument),e=d.employees[0];d.employees=[e];e.inheritsCompanyDefaults=false;e.workload={regimeType:'dayBased',workUnits:5,restUnits:2,weeklyLimitHours:44,defaultShiftDuration:'08:00:00'};e.absences=[];e.inactiveFrom=undefined;d.assignments=[];d.planningPeriod={start:'2026-11-08',end:'2026-11-14'};return d;}
function shift(d,start,end){return {id:crypto.randomUUID(),employeeId:d.employees[0].id,role:d.employees[0].role,start,end,source:'manual',tags:[]};}
test('empty and partial weeks never confirm rest; night carryover is visible',()=>{
 const d=fixture(),e=d.employees[0],week=civilWeek(civilMillis('2026-11-08T00:00:00'));
 assert.equal(weeklyRestSummary(d,e,week).complete,false);
 assert.match(restDayLabel(d,e,'2026-11-09'),/não confirmado/);
 d.assignments=[shift(d,'2026-11-08T22:00:00','2026-11-09T06:00:00')];
 assert.match(restDayLabel(d,e,'2026-11-09'),/anterior/);
 assert.match(restDayLabel(d,e,'2026-11-10'),/Folga prevista/);
 d.planningPeriod.start='2026-11-09';assert.equal(weeklyRestSummary(d,e,week).complete,false);
 e.absences=[{start:'2026-11-10T00:00:00',end:'2026-11-11T00:00:00'}];assert.equal(restDayLabel(d,e,'2026-11-10'),'Ausência registrada');
});
test('seven daily shifts produce actionable rest errors without changing data',()=>{
 const d=fixture();for(let i=8;i<=14;i++)d.assignments.push(shift(d,`2026-11-${String(i).padStart(2,'0')}T08:00:00`,`2026-11-${String(i).padStart(2,'0')}T16:00:00`));
 const before=JSON.stringify(d),issues=weeklyRestIssues(d);
 assert(issues.some(i=>i.code==='weeklyRest'&&i.employeeId===d.employees[0].id));assert(issues.some(i=>i.code==='consecutiveWork'));assert.equal(JSON.stringify(d),before);
 d.employees[0].workload={...d.employees[0].workload,regimeType:'hourBased',workUnits:12,restUnits:36};assert(!weeklyRestIssues(d).some(i=>i.code==='weeklyRest'));
});
test('saved complete weeks expose days off instead of generating preview shifts there',()=>{
 const d=fixture(),{weeklyCoverage}=load('lib/weeklyCoverage.ts');
 d.company.operatingDays=[0,1,2,3,4,5,6];d.company.coverage=[];
 assert(weeklyCoverage(d,'2026-11-10').some(a=>a.planned));
 d.assignments=[shift(d,'2026-11-09T08:00:00','2026-11-09T16:00:00')];
 assert.equal(weeklyCoverage(d,'2026-11-10').length,0);
 assert.equal(weeklyCoverage(d,'2026-11-09')[0].id,d.assignments[0].id);
});
test('Sunday rotation counts overnight overlap and preserves preferences in native round trip',()=>{
 const d=fixture();d.planningPeriod={start:'2026-11-15',end:'2026-11-21'};
 const e=d.employees[0];e.preferences=[{code:'opening',weight:3,description:'A'}];d.employees[0]=setSundayFrequency(e,2);
 d.assignments=[shift(d,'2026-11-07T22:00:00','2026-11-08T06:00:00'),shift(d,'2026-11-15T08:00:00','2026-11-15T16:00:00')];
 assert.equal(sundayFrequency(d.employees[0]),2);assert(weeklyRestIssues(d).some(i=>i.code==='sundayRotation'));
 d.assignments.shift();assert(!weeklyRestIssues(d).some(i=>i.code==='sundayRotation'));
 const parsed=load('lib/documentSchema.ts').documentSchema.parse(JSON.parse(JSON.stringify(d)));assert.equal(sundayFrequency(parsed.employees[0]),2);
 assert.equal(setSundayFrequency(parsed.employees[0],0).preferences[0].code,'opening');
});
