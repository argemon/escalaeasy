using EscalaLivre.Application.Contracts;
using EscalaLivre.Domain.Scheduling;
using EscalaLivre.Infrastructure.Solver;
using System.Text.Json;

namespace EscalaLivre.Adapter.Tests;

public sealed class ScheduleQualityGenerationTests
{
    [Theory]
    [InlineData(true,false)]
    [InlineData(false,true)]
    public void GenerationPreferencesSurviveQueueAndWorkerSerialization(bool preserve,bool balance)
    {
        var request=Request(preserve,balance);var options=ScheduleJson.CreateOptions();
        var json=JsonSerializer.Serialize(request,options);
        var restored=JsonSerializer.Deserialize<ScheduleRequest>(json,options)!;
        Assert.Equal(request.Preferences,restored.Preferences);
        var legacy=JsonSerializer.Deserialize<ScheduleRequest>(JsonSerializer.Serialize(request with {Preferences=null},options),options)!;
        Assert.Null(legacy.Preferences);
    }
    private static ScheduleRequest Request(bool preserve=false,bool balance=false)
    {
        var day=new DateTime(2026,11,15);
        var rule=new WorkloadRule("dayBased",5,2,44,TimeSpan.FromHours(4));
        var company=new CompanyRules("Loja",new(10,0),new(14,0),[DayOfWeek.Sunday],rule,
            new(TimeSpan.Zero,TimeSpan.Zero,TimeSpan.FromHours(2)),TimeSpan.FromHours(11),
            [new("Equipe",DayOfWeek.Sunday,new(10,0),new(14,0),1,Scope:"team")],
            Holidays:[new(DateOnly.FromDateTime(day),"Operação autorizada",false)]);
        var people=Enumerable.Range(0,2).Select(i=>new Employee(Guid.NewGuid(),"Pessoa "+i,"A",true,rule,[],[],[],false,false,[])).ToArray();
        return new(new("1.0.0",Guid.NewGuid(),company,people,[],DateTimeOffset.UtcNow),day,day.AddHours(23).AddMinutes(59).AddSeconds(59),new(preserve,balance));
    }

    [Fact]
    public void BalancesAgainstRecordedHistoryWithoutSacrificingCoverage()
    {
        var request=Request(balance:true);var e=request.Document.Employees[0];
        var history=new ShiftAssignment(Guid.NewGuid(),e.Id,e.Role,request.Start.AddDays(-7).AddHours(10),request.Start.AddDays(-7).AddHours(14),null,"manual",[]);
        request=request with {Document=request.Document with {Assignments=[history]}};
        var result=AdapterComposition.Generate().Execute(request);
        Assert.Empty(result.Gaps);Assert.DoesNotContain(result.Conflicts,c=>c.Severity=="error");
        var selected=Assert.Single(result.Assignments,a=>a.Start>=request.Start);
        Assert.Equal(request.Document.Employees[1].Id,selected.EmployeeId);
        Assert.Equal(JsonSerializer.Serialize(history),JsonSerializer.Serialize(result.Assignments.Single(a=>a.Id==history.Id)));
    }

    [Fact]
    public void BalanceNeverOverridesAvailabilityOrLeavesRequiredCoverageUnfilled()
    {
        var request=Request(balance:true);var e=request.Document.Employees[0];
        var history=new ShiftAssignment(Guid.NewGuid(),e.Id,e.Role,request.Start.AddDays(-7).AddHours(10),request.Start.AddDays(-7).AddHours(14),null,"manual",[]);
        request=request with {Document=request.Document with {Assignments=[history],Employees=[e,request.Document.Employees[1] with {Restrictions=[new(request.Start,request.End)]}]}};
        var result=AdapterComposition.Generate().Execute(request);
        Assert.Empty(result.Gaps);Assert.DoesNotContain(result.Conflicts,c=>c.Severity=="error");
        Assert.Equal(e.Id,Assert.Single(result.Assignments,a=>a.Start>=request.Start).EmployeeId);
    }

    [Fact]
    public void PreservationIncludesExactSavedSecondsOutsideTheGeneratedGrid()
    {
        var request=Request(preserve:true);var e=request.Document.Employees[0];
        var company=request.Document.Company with {OpensAt=new(8,0),ClosesAt=new(18,0),Coverage=[new("Equipe",DayOfWeek.Sunday,new(11,0),new(13,0),1,Scope:"team")]};
        var saved=new ShiftAssignment(Guid.NewGuid(),e.Id,e.Role,request.Start.AddHours(10).AddSeconds(7),request.Start.AddHours(14).AddSeconds(7),null,"manual",[]);
        request=request with {Document=request.Document with {Company=company,Assignments=[saved]}};
        var result=AdapterComposition.Generate().Execute(request);
        Assert.Empty(result.Gaps);Assert.DoesNotContain(result.Conflicts,c=>c.Severity=="error");
        Assert.Contains(result.Assignments,a=>ScheduleQualityPolicy.SameShift(a,saved));
    }

    [Fact]
    public void PreservingAnInvalidSavedShiftIsNeverForced()
    {
        var request=Request(preserve:true);var e=request.Document.Employees[0];
        var saved=new ShiftAssignment(Guid.NewGuid(),e.Id,e.Role,request.Start.AddHours(10),request.Start.AddHours(14),null,"manual",[]);
        request=request with {Document=request.Document with {Assignments=[saved],Employees=[e with {Restrictions=[new(request.Start,request.End)]},request.Document.Employees[1]]}};
        var result=AdapterComposition.Generate().Execute(request);
        Assert.Empty(result.Gaps);Assert.DoesNotContain(result.Conflicts,c=>c.Severity=="error");
        Assert.DoesNotContain(result.Assignments,a=>a.EmployeeId==e.Id);
        Assert.Single(result.Assignments);
    }
}
