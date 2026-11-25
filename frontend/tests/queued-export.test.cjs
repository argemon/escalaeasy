const {test}=require('node:test'),assert=require('node:assert/strict');
const loader=require('./source-loader.cjs');
const reply=data=>Response.json({success:true,data});
const job={id:'private-export-token',state:'queued',position:2,jobsAhead:2,estimatedWaitSeconds:30,estimatedRunSeconds:8,elapsedSeconds:0,pollAfterSeconds:3};
function service(fetch){return loader(undefined,{fetch,setTimeout:fn=>fn()})('lib/queuedExport.ts');}
test('queued export waits in FIFO, reconnects using the same ticket and returns the original result',async()=>{
 const calls=[],progress=[];let polls=0;
 const api=service(async(path,options)=>{
  calls.push([path,options.method]);if(options.method==='POST')return reply(job);
  if(++polls===1)throw new TypeError('offline');
  if(polls===2)return reply({...job,state:'processing'});
  return reply({...job,state:'completed',result:{fileName:'a.pdf',contentBase64:'JVBERi0=',mimeType:'application/pdf'}});
 });
 const result=await api.exportQueued({document:{},format:'pdf'},p=>progress.push(p));
 assert.equal(result.fileName,'a.pdf');assert.equal(calls.filter(c=>c[1]==='POST').length,1);
 assert.equal(progress[1].reconnecting,true);assert.equal(progress.at(-1).state,'completed');
 assert(calls.slice(1).every(c=>c[0]==='/export/jobs/private-export-token'));
});
test('cancel uses DELETE and a cancelled export returns no file',async()=>{
 const calls=[],api=service(async(path,o)=>{calls.push([path,o.method]);return reply({...job,state:'cancelled'});});
 await api.cancelQueuedExport(job.id);assert.deepEqual(calls[0],['/export/jobs/private-export-token','DELETE']);
 assert.equal(await api.exportQueued({document:{},format:'xlsx'}),null);
});
test('queue full, expired ticket, processing failure and large input are explicit errors',async()=>{
 await assert.rejects(service(async()=>Response.json({errors:[{message:'Fila cheia'}]},{status:503})).exportQueued({}),/Fila cheia/);
 let calls=0;await assert.rejects(service(async()=>++calls===1?reply(job):Response.json({errors:[{message:'Expirou'}]},{status:404})).exportQueued({}),/Expirou/);
 await assert.rejects(service(async()=>reply({...job,state:'failed',error:'Limite de memória'})).exportQueued({}),/memória/);
 let posted=false;await assert.rejects(service(async()=>{posted=true;}).exportQueued({pad:'é'.repeat(1024*1024)}),/2 MB/);assert.equal(posted,false);
});
