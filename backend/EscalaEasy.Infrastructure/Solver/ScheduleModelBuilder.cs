using EscalaEasy.Application.Contracts;
using EscalaEasy.Application.Scheduling;
using EscalaEasy.Domain.Scheduling;
using Google.OrTools.Sat;

namespace EscalaEasy.Infrastructure.Solver;

public sealed class ScheduleModelBuilder(IEnumerable<IScheduleConstraintTranslator> translators, ScheduleObjective objective)
{
    private readonly IScheduleConstraintTranslator[] constraints = translators.ToArray();

    internal ScheduleModelContext Build(ScheduleModelInput input, OptimizationBudget budget)
    {
        var context = new ScheduleModelContext(input);
        foreach (var shift in input.CoverageCandidates)
            context.DecisionVars.Add(shift.Id, context.Model.NewBoolVar($"assign_{shift.Id:N}"));
        foreach (var translator in constraints) translator.Apply(context, budget);
        objective.Apply(context);
        return context;
    }
}
