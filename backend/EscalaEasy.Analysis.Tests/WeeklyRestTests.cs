using System.Text.Json;
using EscalaEasy.Domain.Scheduling;

namespace EscalaEasy.Analysis.Tests;

public sealed class WeeklyRestTests
{
    [Fact]
    public void SharedContinuousRestBoundaries()
    {
        using var data=JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"weekly-rest-scenarios.json")));
        foreach(var s in data.RootElement.GetProperty("scenarios").EnumerateArray())
        {
            var shifts=s.GetProperty("shifts").EnumerateArray().Select(a=>new ShiftAssignment(Guid.NewGuid(),Guid.Empty,"A",DateTime.Parse(a[0].GetString()!),DateTime.Parse(a[1].GetString()!),null,"manual",[])).ToArray();
            var gaps=WeeklyRestPolicy.Windows(shifts,DateTime.Parse(s.GetProperty("start").GetString()!),DateTime.Parse(s.GetProperty("end").GetString()!));
            Assert.Equal(s.GetProperty("count").GetInt32(),gaps.Sum(w=>(int)((w.End-w.Start).Ticks/TimeSpan.TicksPerDay)));
        }
    }

    [Fact]
    public void RealRestAndSundayRotationAreIndependentAndNonDestructive()
    {
        var rule=new WorkloadRule("dayBased",5,2,44,TimeSpan.FromHours(8));
        var e=new Employee(Guid.NewGuid(),"A","A",false,rule,[],[],[new("sundayRest2",1,"")],false,true,[]);
        var company=new CompanyRules("A",new(0,0),new(23,59),[],rule,new(TimeSpan.Zero,TimeSpan.FromHours(2),TimeSpan.FromHours(4)),TimeSpan.FromHours(11),[]);
        var start=new DateTime(2026,11,8);
        var shifts=Enumerable.Range(0,7).Select(i=>new ShiftAssignment(Guid.NewGuid(),e.Id,"A",start.AddDays(i).AddHours(8),start.AddDays(i).AddHours(16),null,"manual",[])).ToArray();
        var d=new EscalaEasyDocument("1.0.0",Guid.NewGuid(),company,[e],shifts,DateTimeOffset.UtcNow,new(new(2026,11,8),new(2026,11,14)));
        var issues=WeeklyRestPolicy.Diagnose(d,shifts).ToArray();
        Assert.Contains(issues,i=>i.Code=="weeklyRest"&&i.EmployeeId==e.Id);Assert.Contains(issues,i=>i.Code=="consecutiveWork");
        Assert.DoesNotContain(issues,i=>i.Code=="sundayRotation");
        // Explicit historical Sunday work is required; missing history is never inferred.
        var history=shifts.Append(shifts[0] with {Id=Guid.NewGuid(),Start=start.AddDays(-7).AddHours(-2),End=start.AddDays(-7).AddHours(6)}).ToArray();
        Assert.Contains(WeeklyRestPolicy.Diagnose(d,history),i=>i.Code=="sundayRotation"&&i.Severity=="warning");
        Assert.DoesNotContain(WeeklyRestPolicy.Diagnose(d with {PlanningPeriod=new(new(2026,11,9),new(2026,11,14))},shifts),i=>i.Code=="weeklyRest");
        Assert.Equal(7,d.Assignments.Count);
    }
}
