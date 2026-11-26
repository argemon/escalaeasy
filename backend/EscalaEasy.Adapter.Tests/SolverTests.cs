using EscalaEasy.Application.Contracts;
using EscalaEasy.Domain.Scheduling;
using EscalaEasy.Infrastructure.ImportExport;
using EscalaEasy.Infrastructure.Solver;

namespace EscalaEasy.Adapter.Tests;

public sealed class SolverTests
{
    [Fact]
    public void SolverGeneratesCoverageBlocksAndExplainability()
    {
        var employeeId = Guid.NewGuid();
        var document = new EscalaEasyDocument(
            "1.0.0",
            Guid.NewGuid(),
            new CompanyRules(
                "Clinic",
                new TimeOnly(8, 0),
                new TimeOnly(18, 0),
                [DayOfWeek.Monday],
                new WorkloadRule("dayBased", 7, 0, 44, TimeSpan.FromHours(8)),
                new LunchRule(TimeSpan.FromHours(1), TimeSpan.FromHours(2), TimeSpan.FromHours(4)),
                TimeSpan.FromHours(11),
                [new CoverageRequirement("nurse", DayOfWeek.Monday, new TimeOnly(8, 0), new TimeOnly(11, 0), 1)]),
            [
                new Employee(employeeId, "Ana", "nurse", true, new WorkloadRule("dayBased", 7, 0, 44, TimeSpan.FromHours(8)), [], [], [], false, false, [])
            ],
            [],
            DateTimeOffset.UtcNow);

        var generation = AdapterComposition.Generate();
        ScheduleResult result = generation.Execute(new ScheduleRequest(document, new DateTime(2026, 5, 18), new DateTime(2026, 5, 18, 23, 59, 59)));

        Assert.NotEmpty(result.Assignments);
        Assert.NotEmpty(result.Coverage);
        Assert.NotEmpty(result.Decisions);
        Assert.True(result.CoverageScore > 0);
    }

    [Fact]
    public void NativeSerializerRoundTripsEscalaEasyDocument()
    {
        var document = new EscalaEasyDocument(
            "1.0.0",
            Guid.NewGuid(),
            new CompanyRules(
                "Retail",
                new TimeOnly(9, 0),
                new TimeOnly(22, 0),
                [DayOfWeek.Tuesday],
                new WorkloadRule("dayBased", 6, 1, 44, TimeSpan.FromHours(7)),
                new LunchRule(TimeSpan.FromHours(1), TimeSpan.FromHours(2), TimeSpan.FromHours(4)),
                TimeSpan.FromHours(11),
                []),
            [],
            [],
            DateTimeOffset.UtcNow);

        var serializer = new NativeScheduleSerializer();
        ExportResult exported = serializer.Export(new ExportRequest(document, "escalaeasy"));
        EscalaEasyDocument imported = serializer.Import(new ImportRequest(exported.FileName, exported.ContentBase64, "native"));

        Assert.Equal(document.Id, imported.Id);
        Assert.Equal("1.0.0", imported.SchemaVersion);
        Assert.Equal("Retail", imported.Company.Name);
    }
}
