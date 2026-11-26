using EscalaEasy.Application.Contracts;
using EscalaEasy.Domain.Scheduling;

namespace EscalaEasy.Application.Scheduling;

internal static class ScheduleResultAssembler
{
    public static ScheduleResult From(ScheduleRequest request, ScheduleEvaluation evaluation)
        => new(request.Document.Assignments, evaluation.Conflicts, evaluation.Coverage.Blocks,
            evaluation.Coverage.Gaps, evaluation.HiringSuggestions, evaluation.OptimizationSuggestions,
            ScheduleExplanation.Describe(request.Document.Employees, request.Document.Assignments).ToList(),
            evaluation.Coverage.Score, new("not-run"), evaluation.Workloads);
}
