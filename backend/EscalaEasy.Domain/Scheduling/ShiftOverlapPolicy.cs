namespace EscalaEasy.Domain.Scheduling;

public sealed record ShiftOverlap(ShiftAssignment Assignment, ShiftAssignment Existing);

// Editing policy, not a structural import restriction. Legacy overlaps remain
// recoverable, and an edit may reduce their intersection without creating new overlap.
public static class ShiftOverlapPolicy
{
    public static ShiftOverlap? FindIntroduced(IReadOnlyList<ShiftAssignment> before, IReadOnlyList<ShiftAssignment> after)
    {
        var previous = before.ToDictionary(a => a.Id);
        var people = after.ToLookup(a => a.EmployeeId);
        foreach (var a in after)
        {
            previous.TryGetValue(a.Id,out var oldA);
            if (oldA is not null && SameWindow(oldA,a)) continue;
            foreach (var b in people[a.EmployeeId])
            {
                if (a.Id == b.Id || a.Start >= b.End || b.Start >= a.End) continue;
                previous.TryGetValue(b.Id,out var oldB);
                var start = a.Start > b.Start ? a.Start : b.Start;
                var end = a.End < b.End ? a.End : b.End;
                if (oldA is not null && oldB is not null && oldA.EmployeeId == a.EmployeeId && oldB.EmployeeId == b.EmployeeId
                    && oldA.Start <= start && oldB.Start <= start && oldA.End >= end && oldB.End >= end) continue;
                return new(a,b);
            }
        }
        return null;
    }

    private static bool SameWindow(ShiftAssignment a,ShiftAssignment b)
        => a.EmployeeId == b.EmployeeId && a.Start == b.Start && a.End == b.End;
}
