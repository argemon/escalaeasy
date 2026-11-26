using EscalaEasy.Domain.Scheduling;

namespace EscalaEasy.Application.Contracts;

public sealed record GenerationPreferences(bool PreserveExisting = true, bool BalanceUndesirableShifts = true);
public sealed record ScheduleRequest(EscalaEasyDocument Document, DateTime Start, DateTime End, GenerationPreferences? Preferences = null);

public sealed record ScheduleResult(
    IReadOnlyList<ShiftAssignment> Assignments,
    IReadOnlyList<ScheduleIssue> Conflicts,
    IReadOnlyList<CoverageBlock> Coverage,
    IReadOnlyList<CoverageGap> Gaps,
    IReadOnlyList<HiringSuggestion> HiringSuggestions,
    IReadOnlyList<OptimizationSuggestion> OptimizationSuggestions,
    IReadOnlyList<SolverDecision> Decisions,
    double CoverageScore,
    OptimizationInfo? Optimization = null,
    IReadOnlyList<WorkloadSummary>? Workloads = null);

public sealed record OptimizationInfo(string Status);

public sealed record ImportRequest(string FileName, string ContentBase64, string Format);

public sealed record ExportRequest(EscalaEasyDocument Document, string Format, string? PublicationLabel = null);

public sealed record ExportResult(string FileName, string ContentBase64, string MimeType);
