const {test}=require('node:test'),assert=require('node:assert/strict');
const loader=require('./source-loader.cjs');
function fixture(){const d=structuredClone(require('../../shared/simultaneous-coverage-scenarios.json').scenarios[0].request.document);d.planningPeriod={start:'2026-11-04',end:'2026-11-06'};const e=d.employees[0];d.assignments=[{id:crypto.randomUUID(),employeeId:e.id,role:e.role,start:'2026-11-04T07:30:00',end:'2026-11-04T16:30:00',break:{start:'2026-11-04T11:30:00',end:'2026-11-04T12:30:00'},source:'solver',tags:['private']}];return d;}
test('PDF uses current saved shifts and selected period without archive, validation workflow or staff details',()=>{
 const d=fixture();d.employees[0].restrictions=[{start:'2026-11-04T00:00:00',end:'2026-11-05T00:00:00'}];
 d.publications=[{companyName:'OLD VERSION',shifts:[]}];d.assignments.push({...d.assignments[0],id:crypto.randomUUID(),start:'2026-11-03T07:30:00'});
 const before=JSON.stringify(d),result=loader()('lib/schedulePdf.ts').schedulePdfDocument(d);
 assert.equal(result.assignments.length,1);assert.equal(result.company.name,d.company.name);
 assert.equal(result.publications,undefined);assert.deepEqual([...result.assignments[0].tags],[]);
 assert.equal(result.employees[0].restrictions.length,0);assert.equal(JSON.stringify(d),before);
});
test('one action downloads only the PDF, with no draft or version label',async()=>{
 const d=fixture();d.company.name='Comércio São João';let payload,clicks=0,filename;
 const runtime={Blob,Uint8Array,atob,URL:{createObjectURL:()=> 'blob:test',revokeObjectURL(){}},setTimeout:fn=>fn(),
 document:{createElement:()=>({set download(v){filename=v;},set href(v){},click(){clicks++;}})},
 fetch:async(url,options)=>{payload=JSON.parse(options.body);return {ok:true,json:async()=>({success:true,data:{id:'ticket',state:'completed',result:{contentBase64:btoa('%PDF-test'),mimeType:'application/pdf'}}})};}};
 await loader(undefined,runtime)('lib/schedulePdf.ts').exportSchedulePdf(d);
 assert.equal(payload.format,'pdf');assert.equal(payload.publicationLabel,undefined);assert.equal(clicks,1);
 assert.equal(filename,'Comércio São João - 04-11-2026 a 06-11-2026.pdf');
});
test('empty period gives a useful message without requesting a PDF',async()=>{
 const d=fixture();d.assignments=[];let called=false;
 await assert.rejects(loader(undefined,{fetch:async()=>{called=true;}})('lib/schedulePdf.ts').exportSchedulePdf(d),/Não há turnos salvos/);
 assert.equal(called,false);
});
