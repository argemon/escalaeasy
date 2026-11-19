using EscalaLivre.Domain.Scheduling;
using Google.OrTools.Sat;

namespace EscalaLivre.Infrastructure.Solver;

internal static class ScheduleQualityObjective
{
    internal static void Apply(ScheduleModelContext context)
    {
        var input = context.Input;
        var request = input.Request;
        if (request.Preferences?.PreserveExisting == true)
        {
            var previous = request.Document.Assignments.Where(a => ScheduleWindow.Contains(a, request.Start, request.End))
                .ToLookup(a => (a.EmployeeId, a.Start.Date));
            var terms = input.Candidates.Where(a => previous[(a.EmployeeId, a.Start.Date)].Any(b => ScheduleQualityPolicy.SameShift(a,b)))
                .Select(a => (LinearExpr)context.DecisionVars[a.Id]).ToArray();
            if (terms.Length > 0) context.StabilityObjective = LinearExpr.Sum(terms);
        }
        if (request.Preferences?.BalanceUndesirableShifts != true) return;
        var penalties = new List<LinearExpr>();
        var employees = request.Document.Employees.Where(e => EmploymentPolicy.ActiveOn(e, request.Start))
            .GroupBy(e => ScheduleQualityPolicy.ComparisonGroup(request.Document.Company,e,request.Start));
        foreach (var group in employees)
        foreach (var category in ScheduleQualityPolicy.Categories)
        {
            var comparable = group.Where(e => category != "night" || e.AllowsNightWork).ToArray();
            if (comparable.Length < 2) continue;
            foreach (var employee in comparable)
            {
                var history = input.Preserved.Count(a => a.EmployeeId == employee.Id && a.Start < request.Start.Date
                    && a.Start >= request.Start.Date.AddDays(-ScheduleQualityPolicy.HistoryDays)
                    && ScheduleQualityPolicy.Counts(request.Document.Company,a,category));
                var choices = input.Candidates.Where(a => a.EmployeeId == employee.Id
                    && ScheduleQualityPolicy.Counts(request.Document.Company,a,category)).ToArray();
                // At most one new shift per start date; preserved conflicting history stays countable.
                long upper = history + choices.Select(a => a.Start.Date).Distinct().Count();
                if (upper == 0) continue;
                var count = context.Model.NewIntVar(history,upper,$"quality_{employee.Id}_{category}");
                context.Model.Add(count == LinearExpr.Sum(choices.Select(a => context.DecisionVars[a.Id])) + history);
                var square = context.Model.NewIntVar(0,upper*upper,$"quality_square_{employee.Id}_{category}");
                context.Model.AddMultiplicationEquality(square,count,count);
                penalties.Add(square);
            }
        }
        if (penalties.Count > 0) context.BalanceObjective = LinearExpr.Sum(penalties);
    }
}
