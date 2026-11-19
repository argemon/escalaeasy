using EscalaLivre.Application.Contracts;
using EscalaLivre.Application.Scheduling;
using EscalaLivre.Domain.Scheduling;
using EscalaLivre.Infrastructure.Solver;

namespace EscalaLivre.Adapter.Tests;

public sealed class MinimumIntervalAcceptanceTests
{
    private static (ScheduleModelContext Context, ShiftAssignment First, ShiftAssignment Second, ShiftAssignment Longer) Fixture()
    {
        var day = new DateTime(2026, 11, 4);
        var work = new WorkloadRule("dayBased", 5, 2, 44, TimeSpan.FromHours(8));
        var company = new CompanyRules("Loja", new(7, 30), new(17, 30), [day.DayOfWeek], work,
            new(TimeSpan.FromHours(1), TimeSpan.FromHours(2), TimeSpan.FromHours(4)), TimeSpan.FromHours(11),
            [new("Equipe", day.DayOfWeek, new(7, 30), new(16, 30), 1, Scope: "team")]);
        var employees = new[] { "Edson", "Alex" }.Select(name => new Employee(Guid.NewGuid(), name, "Staff", true,
            work, [], [], [], false, false, [], ShiftStartsAt: new(7, 30))).ToArray();
        ShiftAssignment Shift(int person, double pause, double duration) => new(Guid.NewGuid(), employees[person].Id, "Staff",
            day.AddHours(7.5), day.AddHours(15.5 + duration), new(day.AddHours(pause), day.AddHours(pause + duration)), "solver", []);
        var first = Shift(0, 12.5, 1);
        var second = Shift(1, 11.5, 1);
        var longer = Shift(0, 12.5, 2);
        var overlap = Shift(0, 11.5, 1);
        var request = new ScheduleRequest(new("1.0.0", Guid.NewGuid(), company, employees, [], DateTimeOffset.UtcNow), day, day);
        var budget = new OptimizationBudget();
        var context = AdapterComposition.Builder().Build(new(request, [first, second, longer, overlap], budget), budget);
        return (context, overlap, second, longer);
    }

    private static ScheduleOptimizationResult Result(params ShiftAssignment[] shifts) => new(shifts, shifts, "feasible", []);

    [Fact]
    public void OptimisticMinimumBoundCountsOnlyCoverageUnavailableAcrossAllMinimumCandidates()
    {
        var (original, _, _, _) = Fixture();
        Assert.Equal(0, original.MinimumIntervalCoverageBound());
        var request = original.Input.Request;
        request = request with { Document = request.Document with { Company = request.Document.Company with {
            Coverage = [request.Document.Company.Coverage[0] with { End = new(17, 30) }] } } };
        var budget = new OptimizationBudget();
        var context = AdapterComposition.Builder().Build(new(request, original.Input.Candidates, budget), budget);
        Assert.Equal(TimeSpan.FromHours(1).Ticks, context.MinimumIntervalCoverageBound());
        Assert.Empty(context.Model.Model.Assumptions);
    }

    [Fact]
    public void ABetterMinimumSolutionMustBeCheckedBeforeAcceptingALongerPause()
    {
        var (context, first, second, longer) = Fixture();
        // The incumbent has overlapping one-hour pauses. The wider candidate improves it,
        // but staggering two one-hour pauses already covers the entire requested period.
        var result = MinimumIntervalSearch.Verify(context, new(), new(), Result(first, second), Result(longer, second), 2);
        Assert.Equal(2, result.SelectedAssignments.Count);
        Assert.All(result.SelectedAssignments, a => Assert.Equal(TimeSpan.FromHours(1), a.Break!.End - a.Break.Start));
        Assert.Equal(0, ScheduleCoverage.UncoveredPersonTicks(ScheduleCoverage.Evaluate(context.Input.Request.Document,
            context.Input.Request.Start, context.Input.Request.End, result.Assignments)));
    }

    [Theory]
    [InlineData("timeout")]
    [InlineData("infeasible")]
    public void MissingMinimumBaselineNeverJustifiesALongerPause(string status)
    {
        var (context, _, second, longer) = Fixture();
        var minimum = new ScheduleOptimizationResult([], [], status, []);
        Assert.Same(minimum, MinimumIntervalSearch.Verify(context, new(), new(), minimum, Result(longer, second), 2));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(0.000000001)]
    public void NoVerificationTimeKeepsTheMinimumIncumbent(double seconds)
    {
        var (context, first, second, longer) = Fixture();
        var minimum = Result(first, second);
        Assert.Same(minimum, MinimumIntervalSearch.Verify(context, new(), new(), minimum, Result(longer, second), seconds));
    }

    [Fact]
    public void NecessaryExtensionUsesThirtyMinutesInsteadOfAnHour()
    {
        var (original, _, second, longer) = Fixture();
        var request = original.Input.Request;
        request = request with { Document = request.Document with { Company = request.Document.Company with {
            Coverage = [request.Document.Company.Coverage[0] with { End = new(17, 0) }] } } };
        var shorterExtension = longer with { Id = Guid.NewGuid(), End = longer.End.AddMinutes(-30),
            Break = longer.Break! with { End = longer.Break.End.AddMinutes(-30) } };
        var budget = new OptimizationBudget();
        var context = AdapterComposition.Builder().Build(new(request, original.Input.Candidates.Append(shorterExtension), budget), budget);
        var first = original.Input.Candidates.First(a => a.EmployeeId == longer.EmployeeId
            && a.Break!.Start.Hour == 12 && a.Break.End - a.Break.Start == TimeSpan.FromHours(1));
        var minimum = Result(first, second);
        var result = MinimumIntervalSearch.Verify(context, new(), new(), minimum, Result(longer, second), 2);
        Assert.Equal(30, result.SelectedAssignments.Sum(a => (a.Break!.End - a.Break.Start).TotalMinutes - 60));
        Assert.Equal(0, ScheduleCoverage.UncoveredPersonTicks(ScheduleCoverage.Evaluate(request.Document, request.Start, request.End, result.Assignments)));
    }

    [Fact]
    public void EqualCoverageKeepsMinimumWithoutVerification()
    {
        var (context, _, second, longer) = Fixture();
        var first = context.Input.Candidates.First(a => a.EmployeeId == longer.EmployeeId
            && a.Break!.Start.Hour == 12 && a.Break.End - a.Break.Start == TimeSpan.FromHours(1));
        var minimum = Result(first, second);
        Assert.Same(minimum, MinimumIntervalSearch.Verify(context, new(), new(), minimum, Result(longer, second), 2));
    }
}
