const {test}=require('node:test'),assert=require('node:assert/strict');
const {intervalNotice,intervalNotices,intervalDateRanges}=require('./source-loader.cjs')()('lib/intervalNotice.ts');
const company={opensAt:'07:30:00',closesAt:'17:30:00',lunch:{minimum:'01:00:00',maximum:'02:00:00',preferredAfter:'04:00:00'}};
const employee={name:'Edson'};
const shift={start:'2026-11-04T07:30:00',end:'2026-11-04T17:30:00',break:{start:'2026-11-04T12:30:00',end:'2026-11-04T14:30:00'},tags:['interval-extended-for-coverage','interval-extension-verified']};
test('extended interval identifies the employee, duration, extra time and coverage reason',()=>{
  const notice=intervalNotice(company,employee,shift);
  assert.equal(notice.label,'Intervalo maior: 2h (+1h)');
  assert.match(notice.description,/Edson: mínimo de 1h/);
  assert.match(notice.description,/necessária para atender ao horário de atendimento da empresa/);
});
test('minimum interval, missing pause and missing employee have no notice',()=>{
  assert.equal(intervalNotice(company,employee,{...shift,end:'2026-11-04T16:30:00',break:{...shift.break,end:'2026-11-04T13:30:00'}}),undefined);
  assert.equal(intervalNotice(company,employee,{...shift,break:undefined}),undefined);
  assert.equal(intervalNotice(company,undefined,shift),undefined);
});
test('old saved and fixed intervals never claim proven coverage necessity',()=>{
  const saved=intervalNotice(company,employee,{...shift,tags:[]});
  assert.match(saved.description,/reavaliada/);
  assert.doesNotMatch(saved.description,/necessária/);
  assert.doesNotMatch(intervalNotice(company,employee,{...shift,tags:['interval-extended-for-coverage']}).description,/necessária/);
  const fixed=intervalNotice(company,{...employee,fixedInterval:{start:'12:30:00',end:'14:30:00'}},shift);
  assert.match(fixed.description,/fixado no cadastro/);
  assert.doesNotMatch(fixed.description,/necessária/);
});
test('effective company closure minimum is not an optional extension',()=>{
  assert.equal(intervalNotice({...company,middayClosure:{start:'12:30:00',end:'14:30:00'}},employee,shift),undefined);
});
test('extra duration retains second precision across midnight',()=>{
  const night={...shift,start:'2026-11-04T20:00:00',end:'2026-11-05T05:30:01',break:{start:'2026-11-05T00:00:00',end:'2026-11-05T01:30:01'}};
  assert.equal(intervalNotice(company,employee,night).label,'Intervalo maior: 1h 30min 1s (+30min 1s)');
});

test('verified fixed entry explains the working hours and earlier departure with the minimum pause',()=>{
 const notice=intervalNotice(company,{...employee,shiftStartsAt:'07:30:00'},shift);
 assert.match(notice.description,/entrada fixa às 07:30 e jornada de 8h/);
 assert.match(notice.description,/pausa mínima levaria à saída às 16:30/);
 assert.match(notice.description,/ampliado de 1h para 2h.*horário de atendimento da empresa até 17:30/);
 assert.doesNotMatch(intervalNotice(company,{...employee,shiftStartsAt:'08:30:00'},shift).description,/entrada fixa/);
 assert.doesNotMatch(intervalNotice(company,{...employee,shiftStartsAt:'07:30:00'},{...shift,tags:[]}).description,/levaria à saída/);
});

test('notices group repeated reasons per employee while retaining all dates and different durations',()=>{
 const e={...employee,id:'edson',shiftStartsAt:'07:30:00'};
 const first={...shift,employeeId:e.id};
 const next={...first,start:first.start.replace('11-04','11-05'),end:first.end.replace('11-04','11-05'),break:{start:first.break.start.replace('11-04','11-05'),end:first.break.end.replace('11-04','11-05')}};
 const before=JSON.stringify([first,next]);
 const notices=intervalNotices(company,[e],[next,first,first]);
 assert.equal(notices.length,1);assert.equal(JSON.stringify(notices[0].dates),JSON.stringify(['2026-11-04','2026-11-05']));
 assert.equal(JSON.stringify([first,next]),before);
 const shorter={...first,end:'2026-11-04T17:00:00',break:{...first.break,end:'2026-11-04T14:00:00'}};
 assert.equal(intervalNotices(company,[e],[first,shorter]).length,2);
});

test('date intervals compress consecutive dates without including days that have no notice',()=>{
 assert.equal(intervalDateRanges(['2026-11-06','2026-11-04','2026-11-05','2026-11-04']), 'de 04/11/2026 a 06/11/2026');
 assert.equal(intervalDateRanges(['2026-11-04','2026-11-06']), 'de 04/11/2026 a 04/11/2026; de 06/11/2026 a 06/11/2026');
 assert.equal(intervalDateRanges(['2026-12-31','2027-01-01']), 'de 31/12/2026 a 01/01/2027');
 assert.equal(intervalDateRanges([]), '');
});
