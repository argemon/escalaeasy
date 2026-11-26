using EscalaEasy.Application.Contracts;
using EscalaEasy.Domain.Scheduling;

namespace EscalaEasy.Application.Scheduling;

public sealed record ScheduleOptimizationResult(
    IReadOnlyList<ShiftAssignment> Assignments,
    IReadOnlyList<ShiftAssignment> SelectedAssignments,
    string Status,
    IReadOnlyList<ScheduleIssue> Issues);

// Selects assignments and reports solver status. Evaluation/response assembly belong to Application.
public interface IScheduleOptimizer
{
    ScheduleOptimizationResult Optimize(ScheduleRequest request);
}
