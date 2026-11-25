using EscalaLivre.Domain.Scheduling;
using Google.OrTools.Sat;

namespace EscalaLivre.Infrastructure.Solver;

internal static class SundayRotationObjective
{
    internal static void Apply(ScheduleModelContext context)
    {
        var input=context.Input;var request=input.Request;var penalties=new List<LinearExpr>();
        foreach(var employee in request.Document.Employees)
        {
            int frequency=WeeklyRestPolicy.SundayFrequency(employee);if(frequency==0)continue;
            for(var sunday=ScheduleWindow.WeekStart(request.Start);sunday<=request.End.Date;sunday=sunday.AddDays(7))
            {
                if(!EmploymentPolicy.ActiveOn(employee,sunday.AddDays(1).AddSeconds(-1)))continue;
                var worked=new List<BoolVar>(); bool unknown=false;
                for(int i=0;i<frequency;i++)
                {
                    var day=sunday.AddDays(-i*7);var end=day.AddDays(1);
                    bool historical=input.History[employee.Id].Any(a=>a.Start<end&&a.End>day);
                    var choices=input.Candidates.Where(a=>a.EmployeeId==employee.Id&&a.Start<end&&a.End>day).Select(a=>context.DecisionVars[a.Id]).ToArray();
                    // No history is not evidence of a Sunday off.
                    if(!historical&&day<request.Document.PlanningPeriod?.Start.ToDateTime(TimeOnly.MinValue)){unknown=true;break;}
                    var present=context.Model.NewBoolVar($"sunday_worked_{employee.Id}_{sunday:yyyyMMdd}_{i}");
                    if(historical)context.Model.Add(present==1);
                    else if(choices.Length==0)context.Model.Add(present==0);
                    else context.Model.AddMaxEquality(present,choices);
                    worked.Add(present);
                }
                if(unknown)continue;
                var missed=context.Model.NewBoolVar($"sunday_missed_{employee.Id}_{sunday:yyyyMMdd}");
                context.Model.Add(missed>=LinearExpr.Sum(worked)-(frequency-1));
                penalties.Add(missed);
            }
        }
        if(penalties.Count>0)context.SundayRotationObjective=LinearExpr.Sum(penalties);
    }
}
