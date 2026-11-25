namespace EscalaLivre.Domain.Scheduling;

// Operational rest checks, based on the configured seven-day cycle. No category is inferred.
public static class WeeklyRestPolicy
{
    public static int Required(CompanyRules company, Employee employee)
        => EmployeeSchedulePolicy.Cycle(company,employee) is {} cycle && cycle.CycleDays.Value == 7
            ? cycle.RestDays.Value : 0;

    public static int SundayFrequency(Employee employee)
        => new[] {2,3,4}.FirstOrDefault(n=>employee.Preferences.Any(p=>p.Code==$"sundayRest{n}"&&p.Weight>0));

    public static IReadOnlyList<(DateTime Start, DateTime End)> Windows(IEnumerable<ShiftAssignment> shifts, DateTime start, DateTime end)
    {
        var gaps=new List<(DateTime,DateTime)>(); var cursor=start;
        foreach(var shift in shifts.OrderBy(a=>a.Start))
        {
            var from=shift.Start>start?shift.Start:start;var to=shift.End<end?shift.End:end;
            if(to<=start||from>=end||to<=from)continue;
            if(from>cursor)gaps.Add((cursor,from));
            if(to>cursor)cursor=to;
        }
        if(cursor<end)gaps.Add((cursor,end));
        return gaps;
    }

    public static IEnumerable<ScheduleIssue> Diagnose(EscalaLivreDocument document,IReadOnlyList<ShiftAssignment> assignments)
    {
        if(document.PlanningPeriod is not {} period)yield break;
        var start=period.Start.ToDateTime(TimeOnly.MinValue);var end=period.End.ToDateTime(TimeOnly.MinValue).AddDays(1);
        foreach(var employee in document.Employees)
        {
            var own=assignments.Where(a=>a.EmployeeId==employee.Id).ToArray();var required=Required(document.Company,employee);
            ScheduleIssue Issue(string code,string message,DateTime from,DateTime to,string severity="error")=>new(severity,code,message,from,to,employee.Role,employee.Id);
            for(var week=ScheduleWindow.WeekStart(start);week<end;week=week.AddDays(7))
            {
                if(week<start||week.AddDays(7)>end||!EmploymentPolicy.ActiveOn(employee,week.AddDays(7).AddSeconds(-1))||!own.Any(a=>a.Start<week.AddDays(7)&&a.End>week))continue;
                int count=Windows(own,week,week.AddDays(7)).Sum(w=>(int)((w.End-w.Start).Ticks/TimeSpan.TicksPerDay));
                if(required>0&&count<required)yield return Issue("weeklyRest",$"Repouso semanal insuficiente: {count} de {required} períodos de 24h sem trabalho na semana. Confira os turnos deste período.",week,week.AddDays(7));
            }
            if(required>0)for(var day=start;day.AddDays(7)<=end;day=day.AddDays(1))
            {
                if(EmploymentPolicy.ActiveOn(employee,day.AddDays(7).AddSeconds(-1))&&Windows(own,day,day.AddDays(7)).All(w=>w.End-w.Start<TimeSpan.FromDays(1)))
                    yield return Issue("consecutiveWork","Sete dias sem um período contínuo de 24h de repouso. Confira os turnos deste período.",day,day.AddDays(7));
            }
            int frequency=SundayFrequency(employee);
            if(frequency>0)for(var sunday=ScheduleWindow.WeekStart(start);sunday<end;sunday=sunday.AddDays(7))
            {
                if(sunday.AddDays(1)<=start)continue;
                var from=sunday.AddDays(-(frequency-1)*7);var to=sunday.AddDays(1);
                if(!EmploymentPolicy.ActiveOn(employee,to.AddSeconds(-1)))continue;
                if(Enumerable.Range(0,frequency).Select(i=>from.AddDays(i*7)).All(day=>own.Any(a=>a.Start<day.AddDays(1)&&a.End>day)))
                    yield return Issue("sundayRotation",$"Rodízio de domingos: nenhum domingo inteiro sem trabalho em {frequency} semanas, conforme a preferência cadastrada.",from,to,"warning");
            }
        }
    }
}
