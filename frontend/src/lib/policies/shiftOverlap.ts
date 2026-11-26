import type { EscalaEasyDocument, ShiftAssignment } from "../../types/schedule";
import { civilMillis } from "../civilTime";
import { validCivilDateTime } from "../dateTime";

export function introducedShiftOverlap(before:ShiftAssignment[],after:ShiftAssignment[]) {
  const previous = new Map(before.map(a=>[a.id,a]));
  const people = new Map<string,ShiftAssignment[]>();
  for(const a of after) { const own=people.get(a.employeeId)??[]; own.push(a); people.set(a.employeeId,own); }
  for(const a of after) {
    const oldA=previous.get(a.id);
    if(oldA&&oldA.employeeId===a.employeeId&&oldA.start===a.start&&oldA.end===a.end)continue;
    for(const b of people.get(a.employeeId)??[]) {
      if(a.id===b.id)continue;
      const start=Math.max(civilMillis(a.start),civilMillis(b.start)),end=Math.min(civilMillis(a.end),civilMillis(b.end));
      if(start>=end)continue;
      const oldB=previous.get(b.id);
      if(oldA&&oldB&&oldA.employeeId===a.employeeId&&oldB.employeeId===b.employeeId
        &&civilMillis(oldA.start)<=start&&civilMillis(oldB.start)<=start&&civilMillis(oldA.end)>=end&&civilMillis(oldB.end)>=end)continue;
      return {assignment:a,existing:b};
    }
  }
}
function overlapMessage(d:EscalaEasyDocument,overlap:NonNullable<ReturnType<typeof introducedShiftOverlap>>) {
  const name=d.employees.find(e=>e.id===overlap.assignment.employeeId)?.name??"Esta pessoa";
  const format=(value:string)=>{const [date,time]=value.split("T");return `${date.split("-").reverse().join("/")} às ${time.endsWith(":00")?time.slice(0,5):time}`;};
  return `${name} já possui um turno de ${format(overlap.existing.start)} até ${format(overlap.existing.end)}. Escolha outro horário ou edite esse turno.`;
}
export function assertNoNewShiftOverlap(d:EscalaEasyDocument,next:EscalaEasyDocument) {
  const overlap=introducedShiftOverlap(d.assignments,next.assignments);
  if(overlap)throw Error(overlapMessage(d,overlap));
}
export function shiftOverlapMessage(d:EscalaEasyDocument,assignment:ShiftAssignment) {
  if(!validCivilDateTime(assignment.start)||!validCivilDateTime(assignment.end)||assignment.end<=assignment.start)return undefined;
  const overlap=introducedShiftOverlap(d.assignments,[...d.assignments.filter(a=>a.id!==assignment.id),assignment]);
  return overlap?overlapMessage(d,overlap):undefined;
}
