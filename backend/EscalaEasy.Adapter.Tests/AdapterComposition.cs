using EscalaEasy.Application.Scheduling;
using EscalaEasy.Infrastructure.Solver;
using EscalaEasy.TestSupport;

namespace EscalaEasy.Adapter.Tests;

// Test composition returns real capabilities, never a combined solver/analyzer facade.
internal static class AdapterComposition
{
    internal static ScheduleModelBuilder Builder() => new(
        [new FixedAssignmentConstraints(), new DailyExclusivityConstraints(), new WeeklyIntervalConstraints(), new IntervalTimingConstraints(), new IntervalDurationConstraints(), new RestConstraints(),
         new WeeklyWorkConstraints(), new DayCycleConstraints(), new ContractWorkConstraints(), new CoverageConstraints()], new());
    internal static GenerateSchedule Generate() => new(
        new OrToolsScheduleOptimizer(new(), Builder(), new(), new()), PureComposition.Analyzer());
}
