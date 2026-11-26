using EscalaEasy.Application.Contracts;
using EscalaEasy.Domain.Scheduling;
using EscalaEasy.Infrastructure.Solver;
using Google.OrTools.Sat;

namespace EscalaEasy.Adapter.Tests;

public sealed class WeeklyRestGenerationTests
{
    private static ScheduleRequest Request()
    {
        var rule=new WorkloadRule("dayBased",5,2,44,TimeSpan.FromHours(8));
        var e=new Employee(Guid.NewGuid(),"A","A",false,rule,[],[],[new("sundayRest2",1,"")],false,true,[]);
        var company=new CompanyRules("A",new(0,0),new(23,59),[],rule,new(TimeSpan.Zero,TimeSpan.FromHours(2),TimeSpan.FromHours(4)),TimeSpan.FromHours(11),[]);
        var start=new DateTime(2026,11,8);
        return new(new("1.0.0",Guid.NewGuid(),company,[e],[],DateTimeOffset.UtcNow,new(new(2026,11,8),new(2026,11,14))),start,start.AddDays(6));
    }
    private static ShiftAssignment Shift(ScheduleRequest r,DateTime start,DateTime end)=>new(Guid.NewGuid(),r.Document.Employees[0].Id,"A",start,end,null,"manual",[]);
    private static ScheduleModelContext RestModel(ScheduleRequest request,ShiftAssignment[] shifts)
    {
        var budget=new OptimizationBudget();var context=new ScheduleModelContext(new(request,shifts,budget));
        foreach(var s in context.Input.CoverageCandidates)context.DecisionVars.Add(s.Id,context.Model.NewBoolVar(s.Id.ToString()));
        new RestConstraints().Apply(context,budget);
        new DayCycleConstraints().Apply(context,budget);
        return context;
    }
    [Theory]
    [InlineData(5,true)]
    [InlineData(7,false)]
    public void RealSolverRequiresContinuousWeeklyRest(int days,bool allowed)
    {
        var r=Request();var shifts=Enumerable.Range(0,days).Select(i=>Shift(r,r.Start.AddDays(i).AddHours(8),r.Start.AddDays(i).AddHours(16))).ToArray();
        var context=RestModel(r,shifts);foreach(var s in shifts)context.Model.Add(context.DecisionVars[s.Id]==1);
        var status=new OrToolsExecution().Solve(context.Model).Status;
        Assert.Equal(allowed,status is CpSolverStatus.Optimal or CpSolverStatus.Feasible);
    }
    [Fact]
    public void FiveStartDatesDoNotProveTwoRealRests()
    {
        var r=Request();var s=r.Start;
        var shifts=new[]{Shift(r,s.AddHours(22),s.AddDays(1).AddHours(6)),Shift(r,s.AddDays(2).AddHours(4),s.AddDays(2).AddHours(12)),
            Shift(r,s.AddDays(3).AddHours(4),s.AddDays(3).AddHours(12)),Shift(r,s.AddDays(4).AddHours(4),s.AddDays(4).AddHours(12)),Shift(r,s.AddDays(6),s.AddDays(6).AddHours(8))};
        Assert.Contains(WeeklyRestPolicy.Diagnose(r.Document,shifts),i=>i.Code=="weeklyRest");
        var context=RestModel(r,shifts);foreach(var a in shifts)context.Model.Add(context.DecisionVars[a.Id]==1);
        Assert.Equal(CpSolverStatus.Infeasible,new OrToolsExecution().Solve(context.Model).Status);
    }
    [Fact]
    public void SundayPreferenceRotatesUsingRecordedHistory()
    {
        var r=Request();var history=Shift(r,r.Start.AddDays(-7).AddHours(8),r.Start.AddDays(-7).AddHours(12));
        r=r with {Document=r.Document with {Assignments=[history]}};
        var sunday=Shift(r,r.Start.AddHours(8),r.Start.AddHours(12));var monday=Shift(r,r.Start.AddDays(1).AddHours(8),r.Start.AddDays(1).AddHours(12));
        var budget=new OptimizationBudget();var context=AdapterComposition.Builder().Build(new(r,[sunday,monday],budget),budget);
        context.Model.Add(context.DecisionVars[sunday.Id]+context.DecisionVars[monday.Id]==1);
        Assert.NotNull(context.SundayRotationObjective);context.Model.Minimize(context.SundayRotationObjective!);
        var run=new OrToolsExecution().Solve(context.Model);
        Assert.Equal(CpSolverStatus.Optimal,run.Status);Assert.Equal(0,run.Solver.Value(context.DecisionVars[sunday.Id]));Assert.Equal(1,run.Solver.Value(context.DecisionVars[monday.Id]));
    }
}
