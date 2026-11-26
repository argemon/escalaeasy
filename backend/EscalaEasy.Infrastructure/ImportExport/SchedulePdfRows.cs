using System.Globalization;
using EscalaEasy.Domain.Scheduling;

namespace EscalaEasy.Infrastructure.ImportExport;

internal sealed record SchedulePdfRow(Guid EmployeeId, string Name, string Role, DateTime From, DateTime To, string Hours, string Interval)
{
    internal bool StartsEmployee { get; init; }
    internal bool StartsRole { get; init; }
}

internal static class SchedulePdfRows
{
    internal static IReadOnlyList<SchedulePdfRow> Build(EscalaEasyDocument schedule)
    {
        var people = schedule.Employees.ToDictionary(e => e.Id, e => e.Name);
        var shifts = schedule.Assignments.Where(a => schedule.PlanningPeriod is not {} period
            || (DateOnly.FromDateTime(a.Start) >= period.Start && DateOnly.FromDateTime(a.Start) <= period.End));
        string Name(Guid id) => people.GetValueOrDefault(id, "Pessoa não cadastrada");
        var rows = new List<SchedulePdfRow>();
        foreach (var person in shifts.GroupBy(a => a.EmployeeId)
            .OrderBy(g => Name(g.Key), StringComparer.Create(CultureInfo.GetCultureInfo("pt-BR"), true)).ThenBy(g => g.Key))
        {
            foreach (var shift in person.OrderBy(a => a.Start).ThenBy(a => a.End).ThenBy(a => a.Id))
            {
                var hours = $"{Clock(shift.Start)} - {Clock(shift.End)}{Offset(shift.End, shift.Start)}";
                var interval = shift.Break is {} pause
                    ? $"{Clock(pause.Start)}{Offset(pause.Start, shift.Start)} - {Clock(pause.End)}{Offset(pause.End, shift.Start)}"
                    : "Sem intervalo";
                var previous = rows.LastOrDefault();
                if (previous is not null && previous.EmployeeId == person.Key && previous.To.AddDays(1) == shift.Start.Date
                    && previous.Role == shift.Role && previous.Hours == hours && previous.Interval == interval)
                    rows[^1] = previous with { To = shift.Start.Date };
                else rows.Add(new(person.Key, Name(person.Key), shift.Role, shift.Start.Date, shift.Start.Date, hours, interval));
            }
        }
        return rows.Select((row, index) => row with
        {
            StartsEmployee = index == 0 || rows[index - 1].EmployeeId != row.EmployeeId,
            StartsRole = index == 0 || rows[index - 1].EmployeeId != row.EmployeeId || rows[index - 1].Role != row.Role
        }).ToArray();
    }

    private static string Clock(DateTime value) => value.ToString(value.Second == 0 ? "HH:mm" : "HH:mm:ss", CultureInfo.InvariantCulture);
    private static string Offset(DateTime value, DateTime start)
    {
        int days = (value.Date - start.Date).Days;
        return days == 0 ? "" : $" (+{days}d)";
    }
}
