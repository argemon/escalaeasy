using EscalaEasy.Application.Contracts;

namespace EscalaEasy.Application.Scheduling;

// Read-only evaluation. Both analysis and validation consume this capability.
public interface IScheduleAnalyzer
{
    ScheduleResult Analyze(ScheduleRequest request);
}
