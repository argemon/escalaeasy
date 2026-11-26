using EscalaEasy.Application.Contracts;
using EscalaEasy.Application.Scheduling;
using EscalaEasy.Domain.Scheduling;
using Google.OrTools.Sat;

namespace EscalaEasy.Infrastructure.Solver;

public sealed class DailyExclusivityConstraints : IScheduleConstraintTranslator
{
    public void Apply(ScheduleModelContext context, OptimizationBudget budget)
    {
        var model = context.Model;
        var candidates = context.Input.Candidates;
        var decisionVars = context.DecisionVars;
        foreach (var employeeGroup in candidates.GroupBy(x => new { x.EmployeeId, Day = x.Start.Date }))
        {
            model.Add(LinearExpr.Sum(employeeGroup.Select(x => decisionVars[x.Id])) <= 1);
        }

    }
}
