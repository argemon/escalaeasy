using System.Diagnostics;
using EscalaEasy.Application.Contracts;
using EscalaEasy.Application.Scheduling;
using EscalaEasy.Domain.Scheduling;
using Google.OrTools.Sat;

namespace EscalaEasy.Infrastructure.Solver;

// Exact priorities among found solutions; never sacrifice an earlier incumbent for a lower tier.
internal static class ScheduleObjectiveSearch
{
    internal static ScheduleOptimizationResult Solve(ScheduleModelContext context, OrToolsExecution execution, ScheduleResultReader reader, double seconds)
    {
        var timer = Stopwatch.StartNew();
        var objectives = new List<(LinearExpr Expression, bool Minimize)> { (LinearExpr.Sum(context.CoverageDeficitTerms), true) };
        if (context.IntervalExtension is {} extension) objectives.Add((extension, true));
        if (context.ContractCredit is {} creditObjective) objectives.Add((creditObjective, false));
        if (context.SundayRotationObjective is {} rotation) objectives.Add((rotation, true));
        if (context.StabilityObjective is {} stability) objectives.Add((stability, false));
        if (context.BalanceObjective is {} balance) objectives.Add((balance, true));
        objectives.Add((context.PreferenceObjective!, false));
        ScheduleOptimizationResult? best = null;
        bool proven = true;
        long[]? bestScore = null;
        for (int phase = 0; phase < objectives.Count; phase++)
        {
            double remaining = seconds - timer.Elapsed.TotalSeconds;
            if (remaining <= 0) break;
            var objective = objectives[phase];
            if (objective.Minimize) context.Model.Minimize(objective.Expression); else context.Model.Maximize(objective.Expression);
            var run = execution.Solve(context.Model, phase == objectives.Count - 1 ? remaining : remaining * 0.8);
            if (run.Status is not (CpSolverStatus.Optimal or CpSolverStatus.Feasible))
                return best is null ? reader.Read(context,run) : best with { Status = "feasible" };
            proven &= run.Status == CpSolverStatus.Optimal;
            var candidate = reader.Read(context,run);
            var request=context.Input.Request;
            long uncovered=ScheduleCoverage.UncoveredPersonTicks(ScheduleCoverage.Evaluate(request.Document,request.Start,request.End,candidate.Assignments));
            long extensionSeconds = candidate.SelectedAssignments.Sum(a => context.IntervalExtensionSeconds.GetValueOrDefault(a.Id));
            long credit=ContractWorkPolicy.Summarize(request.Document,request.Start,request.End,candidate.Assignments)?.Sum(w=>w.RecognizedSeconds) ?? 0;
            long[] score = [uncovered,extensionSeconds,-credit,
                .. objectives.Skip(1 + (context.IntervalExtension is null ? 0 : 1) + (context.ContractCredit is null ? 0 : 1))
                    .Select(o => run.Solver.Value(o.Expression) * (o.Minimize ? 1 : -1))];
            // A merely feasible run may not recover even the previous hint. Also, model
            // allocation may undercount actual coverage. Compare canonical incumbents.
            if (bestScore is {} previous && Compare(score,previous) > 0)
                return best! with { Status="feasible" };
            bestScore=score;
            best = candidate with { Status = phase == objectives.Count - 1 && proven ? "optimal" : "feasible" };
            // Improve coverage before spending the remaining budget on lower-priority
            // preferences in a minimum-interval attempt that will need a wider search.
            if (phase == 0 && uncovered > 0 && context.PreferMinimumIntervals is {} minimum
                && context.Model.Model.Assumptions.Contains(minimum.Index)) return best;
            // A conditional lock belongs to this attempt only. ClearAssumptions before the
            // early-lunch fallback releases these bounds, so coverage can improve there.
            var guard = context.Model.NewBoolVar($"schedule_priority_{phase}_{Guid.NewGuid():N}");
            context.Model.Add(objective.Expression == run.Solver.Value(objective.Expression)).OnlyEnforceIf(guard);
            context.Model.AddAssumption(guard);
            context.Model.ClearHints();
            foreach (var decision in context.DecisionVars) context.Model.AddHint(decision.Value,run.Solver.Value(decision.Value));
        }
        return best ?? reader.Read(context,new(CpSolverStatus.Unknown,new CpSolver()));
    }

    private static int Compare(long[] a,long[] b)
    {
        for (int i=0;i<a.Length;i++) if(a[i]!=b[i]) return a[i].CompareTo(b[i]);
        return 0;
    }
}
