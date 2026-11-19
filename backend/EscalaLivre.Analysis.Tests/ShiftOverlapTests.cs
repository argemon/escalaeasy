using System.Text.Json;
using EscalaLivre.Domain.Scheduling;

namespace EscalaLivre.Analysis.Tests;

public sealed class ShiftOverlapTests
{
    [Fact]
    public void SharedOverlapCasesPreserveLegacyAndBlockNewIntersections()
    {
        using var json=JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"shift-overlap-scenarios.json")));
        var ids=new Dictionary<string,Guid>();
        Guid Id(string value) { if(!ids.TryGetValue(value,out var id))ids[value]=id=Guid.NewGuid();return id; }
        foreach(var s in json.RootElement.GetProperty("scenarios").EnumerateArray())
        {
            ShiftAssignment[] Shifts(string key)=>s.GetProperty(key).EnumerateArray().Select(a=>new ShiftAssignment(
                Id(a.GetProperty("id").GetString()!),Id(a.GetProperty("employee").GetString()!),"A",
                DateTime.Parse(a.GetProperty("start").GetString()!),DateTime.Parse(a.GetProperty("end").GetString()!),null,"manual",[])).ToArray();
            Assert.Equal(s.GetProperty("blocked").GetBoolean(),ShiftOverlapPolicy.FindIntroduced(Shifts("before"),Shifts("after")) is not null);
        }
    }
}
