using EscalaEasy.Application.Contracts;
using EscalaEasy.Application.Scheduling;
using EscalaEasy.Domain.Scheduling;
using Google.OrTools.Sat;

namespace EscalaEasy.Infrastructure.Solver;

public sealed class ScheduleModelContext(ScheduleModelInput input)
{
    public ScheduleModelInput Input { get; } = input;
    internal CpModel Model { get; } = new();
    internal Dictionary<Guid, BoolVar> DecisionVars { get; } = new();
    internal List<LinearExpr> CoverageDeficitTerms { get; } = [];
    internal LinearExpr? ContractCredit { get; set; }
    internal LinearExpr? PreferenceObjective { get; set; }
    internal LinearExpr? StabilityObjective { get; set; }
    internal LinearExpr? SundayRotationObjective { get; set; }
    internal LinearExpr? BalanceObjective { get; set; }
    internal List<LinearExpr> ShortageTerms { get; } = [];
    internal List<LinearExpr> OccupancyTerms { get; } = [];
    internal Dictionary<Guid, long> IntervalExtensionSeconds { get; } = new();
    internal LinearExpr? IntervalExtension { get; set; }
    internal BoolVar? PreferMinimumIntervals { get; set; }
    internal BoolVar? PreferUnanticipatedIntervals { get; set; }

    // Ignoring exclusivity, workload and rest can only overestimate attainable coverage.
    internal long MinimumIntervalCoverageBound()
    {
        var request = Input.Request;
        var optimistic = Input.CoverageCandidates.Where(a => !IntervalExtensionSeconds.ContainsKey(a.Id)).ToArray();
        return ScheduleCoverage.UncoveredPersonTicks(ScheduleCoverage.Evaluate(request.Document,
            request.Start, request.End, optimistic));
    }
}

public interface IScheduleConstraintTranslator
{
    void Apply(ScheduleModelContext context, OptimizationBudget budget);
}
