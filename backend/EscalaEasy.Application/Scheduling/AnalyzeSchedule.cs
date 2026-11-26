using EscalaEasy.Application.Contracts;

namespace EscalaEasy.Application.Scheduling;

public sealed class AnalyzeSchedule(IScheduleAnalyzer analyzer)
{
    public ScheduleResult Execute(ScheduleRequest request) => analyzer.Analyze(request);
}
