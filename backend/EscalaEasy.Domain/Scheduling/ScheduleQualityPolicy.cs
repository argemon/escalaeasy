namespace EscalaEasy.Domain.Scheduling;

// Planning preferences only: these never authorize an assignment or invalidate a file.
public static class ScheduleQualityPolicy
{
    public const int HistoryDays = 28;
    public static readonly IReadOnlyList<string> Categories = Array.AsReadOnly(new[] { "sunday", "holiday", "closing", "night" });

    public static bool SameShift(ShiftAssignment a, ShiftAssignment b) => a.EmployeeId == b.EmployeeId
        && a.Role == b.Role && a.RoleId == b.RoleId && a.Start == b.Start && a.End == b.End && a.Break == b.Break;

    public static bool Counts(CompanyRules company, ShiftAssignment shift, string category) => category switch
    {
        "sunday" => shift.Start.DayOfWeek == DayOfWeek.Sunday,
        "holiday" => company.Holidays?.Any(h => h.Date == DateOnly.FromDateTime(shift.Start)) == true || LaborRules.ClosedHoliday(company,shift.Start),
        "closing" => shift.End == ScheduleRules.EndOn(shift.Start.Date, company.OpensAt, company.ClosesAt),
        "night" => ShiftEligibilityPolicy.IsNight(shift.Start, shift.End),
        _ => false
    };

    public static string ComparisonGroup(CompanyRules company, Employee employee, DateTime on)
    {
        var e = EmploymentPolicy.On(employee, on);
        var rule = EffectiveWorkPolicy.RuleFor(company, e);
        return $"{e.RoleId?.ToString() ?? e.Role}|{rule.WeeklyPlan?.ContractedSeconds ?? (long)rule.DefaultShiftDuration.TotalSeconds}|{rule.WeeklyPlan is not null}";
    }
}
