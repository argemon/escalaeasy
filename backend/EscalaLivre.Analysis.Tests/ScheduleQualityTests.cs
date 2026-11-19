using System.Text.Json;
using EscalaLivre.Domain.Scheduling;

namespace EscalaLivre.Analysis.Tests;

public sealed class ScheduleQualityTests
{
    [Fact]
    public void SharedQualityClassificationUsesCivilEntryDateAndExactNightBoundary()
    {
        using var json=JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"schedule-quality-scenarios.json")));
        var rule=new WorkloadRule("dayBased",5,2,44,TimeSpan.FromHours(8));
        foreach(var s in json.RootElement.GetProperty("scenarios").EnumerateArray())
        {
            var company=new CompanyRules("A",TimeOnly.Parse(s.GetProperty("opensAt").GetString()!),TimeOnly.Parse(s.GetProperty("closesAt").GetString()!),[],rule,new(TimeSpan.Zero,TimeSpan.FromHours(2),TimeSpan.FromHours(4)),TimeSpan.FromHours(11),[],
                Holidays:s.TryGetProperty("holiday",out var holiday)?[new(DateOnly.Parse(holiday.GetString()!),"H",false)]:[]);
            var shift=new ShiftAssignment(Guid.NewGuid(),Guid.NewGuid(),"A",DateTime.Parse(s.GetProperty("start").GetString()!),DateTime.Parse(s.GetProperty("end").GetString()!),null,"manual",[]);
            Assert.Equal(s.GetProperty("expected").EnumerateArray().Select(x=>x.GetString()),ScheduleQualityPolicy.Categories.Where(c=>ScheduleQualityPolicy.Counts(company,shift,c)));
            Assert.True(ScheduleQualityPolicy.SameShift(shift,shift with {Id=Guid.NewGuid(),Tags=["x"]}));
            Assert.False(ScheduleQualityPolicy.SameShift(shift,shift with {End=shift.End.AddSeconds(1)}));
        }
    }
}
