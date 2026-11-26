using EscalaEasy.Application.Scheduling;
using EscalaEasy.Domain.Scheduling;

namespace EscalaEasy.TestSupport;

internal static class PureComposition
{
    internal static IScheduleAnalyzer Analyzer() => new ScheduleAnalyzer(new ScheduleEvaluator());
    internal static AnalyzeSchedule Analyze() => new(Analyzer());
    internal static ValidateSchedule Validate() => new(Analyzer());
}
