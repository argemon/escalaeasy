using EscalaEasy.Application.Scheduling;
using EscalaEasy.Domain.Scheduling;
using Google.OrTools.Sat;
using System.Diagnostics;

namespace EscalaEasy.Infrastructure.Solver;

internal static class MinimumIntervalSearch
{
    internal static ScheduleOptimizationResult Verify(ScheduleModelContext context, OrToolsExecution execution,
        ScheduleResultReader reader, ScheduleOptimizationResult minimum, ScheduleOptimizationResult extended, double seconds)
    {
        var request = context.Input.Request;
        long Uncovered(ScheduleOptimizationResult result) => ScheduleCoverage.UncoveredPersonTicks(
            ScheduleCoverage.Evaluate(request.Document, request.Start, request.End, result.Assignments));
        bool Succeeded(ScheduleOptimizationResult result) => result.Status is "optimal" or "feasible";
        if (!Succeeded(minimum) || !Succeeded(extended) || seconds <= 0
            || Uncovered(extended) >= Uncovered(minimum) || context.IntervalExtension is null) return minimum;

        var timer = Stopwatch.StartNew();
        var deficit = LinearExpr.Sum(context.CoverageDeficitTerms);
        var target = Uncovered(extended) / TimeSpan.TicksPerSecond;
        // Search-local locks from earlier objectives must not make this proof artificially infeasible.
        context.Model.ClearAssumptions();
        var coverage = context.Model.NewBoolVar("verify_interval_coverage");
        context.Model.Add(deficit <= target).OnlyEnforceIf(coverage);

        void Prepare(bool minimumOnly, ScheduleOptimizationResult hint)
        {
            context.Model.ClearAssumptions();
            context.Model.AddAssumption(coverage);
            if (context.PreferMinimumIntervals is {} duration)
                context.Model.AddAssumption(minimumOnly ? duration : duration.Not());
            // An earlier one-hour pause is preferable to an unnecessary longer pause.
            if (context.PreferUnanticipatedIntervals is {} timing) context.Model.AddAssumption(timing.Not());
            context.Model.ClearHints();
            var selected = hint.SelectedAssignments.Concat(context.Input.FixedCoverage).Select(a => a.Id).ToHashSet();
            foreach (var decision in context.DecisionVars)
                context.Model.AddHint(decision.Value, selected.Contains(decision.Key) ? 1 : 0);
        }

        if (context.MinimumIntervalCoverageBound() <= Uncovered(extended))
        {
            Prepare(true, minimum);
            context.Model.Minimize(LinearExpr.Constant(0));
            var counterfactual = execution.Solve(context.Model, seconds / 2);
            if (counterfactual.Status is CpSolverStatus.Optimal or CpSolverStatus.Feasible)
            {
                var shorter = reader.Read(context, counterfactual);
                return Uncovered(shorter) <= Uncovered(extended) ? shorter with { Status = "feasible" } : minimum;
            }
            // A timeout is not evidence that the longer interval is needed.
            if (counterfactual.Status != CpSolverStatus.Infeasible) return minimum;
        }

        double remaining = seconds - timer.Elapsed.TotalSeconds;
        if (remaining <= 0) return minimum;
        Prepare(false, extended);
        context.Model.Minimize(context.IntervalExtension);
        var leastExtension = execution.Solve(context.Model, remaining);
        // Accept only the least total extension capable of this coverage, proven within the budget.
        if (leastExtension.Status != CpSolverStatus.Optimal) return minimum;
        var verified = reader.Read(context, leastExtension) with { Status = "feasible" };
        return Uncovered(verified) <= Uncovered(extended) ? verified : minimum;
    }
}
