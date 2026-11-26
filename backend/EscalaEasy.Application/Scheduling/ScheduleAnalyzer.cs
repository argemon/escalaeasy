using EscalaEasy.Application.Contracts;
using EscalaEasy.Domain.Scheduling;

namespace EscalaEasy.Application.Scheduling;

public sealed class ScheduleAnalyzer(ScheduleEvaluator evaluator) : IScheduleAnalyzer
{
    public ScheduleResult Analyze(ScheduleRequest request)
        => ScheduleResultAssembler.From(request,
            evaluator.Evaluate(request.Document, request.Start, request.End, request.Document.Assignments));
}
