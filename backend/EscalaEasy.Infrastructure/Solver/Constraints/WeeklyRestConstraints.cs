using EscalaEasy.Domain.Scheduling;
using Google.OrTools.Sat;

namespace EscalaEasy.Infrastructure.Solver;

internal static class WeeklyRestConstraints
{
    internal static void Apply(ScheduleModelContext context, OptimizationBudget budget)
    {
        var input=context.Input;var request=input.Request;
        if(request.Document.PlanningPeriod is not {} period)return;
        var from=period.Start.ToDateTime(TimeOnly.MinValue);var to=period.End.ToDateTime(TimeOnly.MinValue).AddDays(1);
        foreach(var employee in request.Document.Employees)
        {
            int required=WeeklyRestPolicy.Required(request.Document.Company,employee);if(required==0)continue;
            var shifts=input.Candidates.Where(a=>a.EmployeeId==employee.Id).ToArray();
            var history=input.History[employee.Id].ToArray();
            // The day-cycle translator already guarantees whole free dates when
            // every shift ends on its entry date. Only overnight work needs
            // additional continuous-time constraints.
            if(shifts.Concat(history).All(a=>a.End<=a.Start.Date.AddDays(1)))continue;
            var windows=new Dictionary<DateTime,int>();
            for(var week=ScheduleWindow.WeekStart(from);week.AddDays(7)<=to;week=week.AddDays(7))
                if(week>=from)windows[week]=required;
            for(var day=from;day.AddDays(7)<=to;day=day.AddDays(1))windows.TryAdd(day,1);
            foreach(var (start,count) in windows)
            {
                var end=start.AddDays(7);
                if(end<=request.Start.Date||start>=request.End.Date.AddDays(1)||!EmploymentPolicy.ActiveOn(employee,end.AddSeconds(-1)))continue;
                var rests=new List<IntervalVar>();
                for(int i=0;i<count;i++)
                {
                    budget.Spend();
                    var at=context.Model.NewIntVar(0,6*86400,$"rest_start_{employee.Id}_{start:yyyyMMdd}_{i}");
                    var rest=context.Model.NewIntervalVar(at,86400,at+86400,$"weekly_rest_{employee.Id}_{start:yyyyMMdd}_{i}");
                    rests.Add(rest);
                    foreach(var shift in shifts.Concat(history).Where(a=>a.Start<end&&a.End>start))
                    {
                        budget.Spend();
                        long s=(long)(shift.Start-start).TotalSeconds,e=(long)(shift.End-start).TotalSeconds;
                        var work=context.DecisionVars.TryGetValue(shift.Id,out var selected)
                            ? context.Model.NewOptionalIntervalVar(s,e-s,e,selected,$"rest_work_{shift.Id}_{start:yyyyMMdd}_{i}")
                            : context.Model.NewIntervalVar(s,e-s,e,$"rest_history_{shift.Id}_{start:yyyyMMdd}_{i}");
                        // Pairwise separation preserves overlapping inherited shifts.
                        context.Model.AddNoOverlap(new[]{rest,work});
                    }
                }
                context.Model.AddNoOverlap(rests);
            }
        }
    }
}
