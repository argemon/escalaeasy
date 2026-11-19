using EscalaLivre.Domain.Scheduling;
using EscalaLivre.Infrastructure.ImportExport;

namespace EscalaLivre.Adapter.Tests;

public sealed class SchedulePdfRowsTests
{
    private static EscalaLivreDocument Document()
    {
        var work=new WorkloadRule("dayBased",5,2,44,TimeSpan.FromHours(8));
        var employees=new[]{"Edson","Alex"}.Select(name=>new Employee(Guid.NewGuid(),name,"Equipe",true,work,[],[],[],false,false,[])).ToArray();
        var company=new CompanyRules("Argemon Games",new(7,30),new(17,30),[DayOfWeek.Wednesday],work,
            new(TimeSpan.FromHours(1),TimeSpan.FromHours(2),TimeSpan.FromHours(4)),TimeSpan.FromHours(11),[]);
        var shifts=employees.SelectMany(e=>Enumerable.Range(4,3).Select(day=>new ShiftAssignment(Guid.NewGuid(),e.Id,e.Role,
            new(2026,11,day,7,30,0),new(2026,11,day,16,30,0),new(new(2026,11,day,11,30,0),new(2026,11,day,12,30,0)),"solver",[]))).ToArray();
        return new("1.0.0",Guid.NewGuid(),company,employees,shifts,DateTimeOffset.UtcNow,new(new(2026,11,4),new(2026,11,7)));
    }

    [Fact]
    public void ThreeEqualDaysBecomeOneRangePerPerson()
    {
        var rows=SchedulePdfRows.Build(Document());
        Assert.Equal(new[]{"Alex","Edson"},rows.Select(r=>r.Name));
        Assert.All(rows,r=>{Assert.Equal(new DateTime(2026,11,4),r.From);Assert.Equal(new DateTime(2026,11,6),r.To);
            Assert.Equal("07:30 - 16:30",r.Hours);Assert.Equal("11:30 - 12:30",r.Interval);});
    }

    [Theory]
    [InlineData("dayOff")]
    [InlineData("hours")]
    [InlineData("pause")]
    [InlineData("role")]
    public void DaysOffAndScheduleChangesAreNeverHidden(string change)
    {
        var d=Document();var shifts=d.Assignments.ToArray();
        var middle=shifts[1];
        if(change=="dayOff")shifts=shifts.Where(a=>a.Id!=middle.Id).ToArray();
        else shifts[1]=change switch {
            "hours"=>middle with{End=middle.End.AddMinutes(30)},
            "pause"=>middle with{Break=middle.Break! with{End=middle.Break.End.AddMinutes(30)}},
            _=>middle with{Role="Monitor"}
        };
        var rows=SchedulePdfRows.Build(d with{Assignments=shifts}).Where(r=>r.EmployeeId==middle.EmployeeId).ToArray();
        Assert.Equal(change=="dayOff"?2:3,rows.Length);
        Assert.All(rows,r=>Assert.Equal(r.From,r.To));
    }

    [Fact]
    public void SameRoleIsShownOnceAcrossSeparateDateRangesForEachEmployee()
    {
        var d=Document();
        var rows=SchedulePdfRows.Build(d with{Assignments=d.Assignments.Where(a=>a.Start.Day!=5).ToArray()});
        Assert.Equal(4,rows.Count);
        foreach(var person in rows.GroupBy(r=>r.EmployeeId))
        {
            Assert.True(person.First().StartsEmployee);
            Assert.True(person.First().StartsRole);
            Assert.All(person.Skip(1),r=>{Assert.False(r.StartsEmployee);Assert.False(r.StartsRole);});
        }
    }

    [Fact]
    public void RoleChangesAndReturnsRemainVisibleWithoutRepeatingTheName()
    {
        var d=Document();var shifts=d.Assignments.ToArray();
        shifts[1]=shifts[1] with{Role="Monitor"};
        var rows=SchedulePdfRows.Build(d with{Assignments=shifts}).Where(r=>r.EmployeeId==shifts[1].EmployeeId).ToArray();
        Assert.Equal(new[]{"Equipe","Monitor","Equipe"},rows.Select(r=>r.Role));
        Assert.All(rows,r=>Assert.True(r.StartsRole));
        Assert.Equal(new[]{true,false,false},rows.Select(r=>r.StartsEmployee));
    }

    [Fact]
    public void NightOffsetsSecondsAndPeriodBoundariesArePreserved()
    {
        var d=Document();var e=d.Employees[0];
        var shifts=Enumerable.Range(3,6).Select(day=>new ShiftAssignment(Guid.NewGuid(),e.Id,e.Role,
            new(2026,11,day,22,0,1),new(2026,11,day+1,6,0,2),new(new(2026,11,day+1,2,0,3),new(2026,11,day+1,3,0,4)),"manual",[])).ToArray();
        var row=Assert.Single(SchedulePdfRows.Build(d with{Assignments=shifts}));
        Assert.Equal(new DateTime(2026,11,4),row.From);Assert.Equal(new DateTime(2026,11,7),row.To);
        Assert.Equal("22:00:01 - 06:00:02 (+1d)",row.Hours);
        Assert.Equal("02:00:03 (+1d) - 03:00:04 (+1d)",row.Interval);
    }
}
