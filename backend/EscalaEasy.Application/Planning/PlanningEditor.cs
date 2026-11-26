using EscalaEasy.Application.Contracts;
using EscalaEasy.Application.Scheduling;
using EscalaEasy.Application.Validation;
using EscalaEasy.Domain.Planning;
using EscalaEasy.Domain.Scheduling;

namespace EscalaEasy.Application.Planning;

public sealed record PlanningEditResult(EscalaEasyDocument Document, ScheduleResult Analysis);

// Explicit application commands. They do not add HTTP endpoints or silently repair business conflicts.
public sealed class PlanningEditor(IScheduleAnalyzer analyzer)
{
    private PlanningEditResult Edit(ScheduleRequest request, Action<PlanningDocumentMapping> command, DateTimeOffset updatedAt, bool writePeriod = false)
    {
        var mapping = PlanningDocumentMapping.Open(request);
        command(mapping);
        foreach(var a in mapping.ToDocument().Assignments) {
            var old=request.Document.Assignments.FirstOrDefault(p=>p.Id==a.Id);
            var employee=mapping.Settings.Employees.First(e=>e.Id==a.EmployeeId);
            if((old is null || old.EmployeeId!=a.EmployeeId || old.Start.Date!=a.Start.Date) && !EmploymentPolicy.ActiveOn(employee,a.Start))
                throw new InvalidOperationException("Pessoa inativa na data inicial do turno.");
        }
        var document = mapping.ToDocument(writePeriod) with { UpdatedAt = updatedAt.ToUniversalTime() };
        if (DocumentSafety.Error(document) is { } error) throw new FormatException(error);
        if (ShiftOverlapPolicy.FindIntroduced(request.Document.Assignments,document.Assignments) is { } overlap)
            throw new InvalidOperationException($"Turno coincide com outro turno da mesma pessoa, de {overlap.Existing.Start:dd/MM/yyyy HH:mm:ss} até {overlap.Existing.End:dd/MM/yyyy HH:mm:ss}. Edite o turno existente ou escolha outro horário.");
        var period = mapping.Schedule.Period;
        var analysis = analyzer.Analyze(new(document, period.Start.ToDateTime(TimeOnly.MinValue), period.End.ToDateTime(new(23,59,59))));
        return new(document, analysis);
    }

    public PlanningEditResult Assign(ScheduleRequest request, ShiftAssignment assignment, DateTimeOffset updatedAt)
        => Edit(request, m => m.Schedule.Assign(m.ImportAssignment(assignment)), updatedAt);

    public PlanningEditResult Update(ScheduleRequest request, Guid id, ShiftAssignment replacement, DateTimeOffset updatedAt)
        => Edit(request, m => m.Schedule.Update(id, m.ImportAssignment(replacement)), updatedAt);

    public PlanningEditResult Remove(ScheduleRequest request, Guid id, DateTimeOffset updatedAt)
        => Edit(request, m => m.Schedule.Remove(id), updatedAt);

    public PlanningEditResult Move(ScheduleRequest request, Guid id, CivilWindow target, DateTimeOffset updatedAt)
        => Edit(request, m => m.Schedule.Move(id, target), updatedAt);

    public PlanningEditResult ReplacePeriod(ScheduleRequest request, IReadOnlyList<ShiftAssignment> replacements, DateTimeOffset updatedAt)
        => Edit(request, m => m.Schedule.ReplacePeriod(replacements.Select(m.ImportAssignment)), updatedAt);

    public PlanningEditResult ChangePeriod(ScheduleRequest request, CivilPlanningPeriod period, DateTimeOffset updatedAt)
        => Edit(request, m => m.Schedule.ChangePeriod(period), updatedAt, writePeriod: true);

    public PlanningEditResult ChangeCompany(ScheduleRequest request, CompanyRules company, DateTimeOffset updatedAt)
        => Edit(request, m => m.ChangeCompany(company), updatedAt);

    public PlanningEditResult ChangeEmployee(ScheduleRequest request, Guid id, Employee employee, DateTimeOffset updatedAt)
        => Edit(request, m => m.ChangeEmployee(id, employee), updatedAt);
}
