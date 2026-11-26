using System.Security.Cryptography;
using EscalaEasy.Application.Contracts;

namespace EscalaEasy.Api;

public interface IExportRunner
{
    Task<ExportResult> RunAsync(ExportRequest request, CancellationToken cancellation);
}

public sealed record ExportJobProgress(string Id, string State, int Position, int JobsAhead,
    int EstimatedWaitSeconds, int EstimatedRunSeconds, int ElapsedSeconds, int ProcessingSeconds,
    int WorkerTimeoutSeconds, int PollAfterSeconds, ExportResult? Result = null, string? Error = null);

// One process, bounded memory, FIFO. Tokens are bearer secrets; never log job IDs or documents.
public sealed class ExportJobQueue(IExportRunner runner, ProtectionOptions options,
    ILogger<ExportJobQueue> logger, TimeProvider? timeProvider = null) : BackgroundService
{
    private sealed class Job(string id, byte[] request, DateTimeOffset created)
    {
        public string Id { get; } = id;
        public byte[]? Payload = request;
        public DateTimeOffset Created = created;
        public DateTimeOffset? Started, Finished;
        public string State = "queued";
        public string? Error;
        public CancellationTokenSource? Cancellation;
        public long PayloadCharge = ExportJobPayload.MaximumBytes;
    }

    private readonly object gate = new();
    private readonly List<Job> jobs = [];
    // Coalesced wake-ups: cancelled queued jobs cannot accumulate semaphore permits.
    private readonly SemaphoreSlim available = new(0, 1);
    private readonly TimeProvider clock = timeProvider ?? TimeProvider.System;
    private long payloadCharge;
    private double averageSeconds = 8;
    private bool stopping;

    internal (int Tickets, long PayloadBytes) RetentionUsage
    {
        get { lock (gate) return (jobs.Count, payloadCharge); }
    }

    public ExportJobProgress Enqueue(ExportRequest request)
    {
        ExportJobPayload.Validate(request);
        lock (gate)
        {
            Prune();
            if (stopping || jobs.Count >= options.ExportJobRetentionLimit
                || jobs.Count(j => j.State == "queued") >= options.ExportJobQueueLimit
                || payloadCharge + ExportJobPayload.MaximumBytes > options.ExportJobPayloadBudgetMb * 1024L * 1024)
                throw new ApiFailure(503, "queueFull", "A fila de exportação está cheia. Aguarde um pouco e tente novamente.");
            var job = new Job(Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant(),
                ExportJobPayload.Serialize(request), clock.GetUtcNow());
            jobs.Add(job);
            payloadCharge += job.PayloadCharge;
            Signal();
            return Snapshot(job);
        }
    }

    public ExportJobProgress Get(string id)
    {
        ExportJobProgress progress;
        byte[]? result;
        lock (gate)
        {
            Prune();
            var job = Find(id);
            progress = Snapshot(job);
            result = job.State == "completed" ? job.Payload : null;
        }
        return WithResult(progress, result);
    }

    public ExportJobProgress Cancel(string id)
    {
        ExportJobProgress progress;
        byte[]? result;
        lock (gate)
        {
            Prune();
            var job = Find(id);
            if (job.State == "queued")
            {
                Finish(job, "cancelled", null, null);
            }
            // A running slot stays occupied until the calculation process really exits.
            if (job.State == "processing") job.Cancellation!.Cancel();
            progress = Snapshot(job);
            result = job.State == "completed" ? job.Payload : null;
        }
        return WithResult(progress, result);
    }

    // Compatibility callers use the same FIFO and worker as asynchronous exports.
    public async Task<ExportResult> RunLegacyAsync(ExportRequest request, CancellationToken cancellation)
    {
        var progress=Enqueue(request);
        try
        {
            while(true)
            {
                cancellation.ThrowIfCancellationRequested();
                progress=Get(progress.Id);
                if(progress.State=="completed")return progress.Result!;
                if(progress.State=="failed")throw new ApiFailure(422,"exportFailed",progress.Error??"Não foi possível exportar.");
                if(progress.State=="cancelled")throw new OperationCanceledException(cancellation);
                await Task.Delay(100,cancellation);
            }
        }
        catch(OperationCanceledException){Cancel(progress.Id);throw;}
    }

    private Job Find(string id) => jobs.FirstOrDefault(j => j.Id == id)
        ?? throw new ApiFailure(404, "jobExpired", "Este pedido expirou ou o servidor foi reiniciado. Solicite a exportação novamente.");

    private void Prune()
    {
        var expiry = clock.GetUtcNow().AddMinutes(-2);
        foreach (var job in jobs.Where(j => j.Finished <= expiry).ToArray())
        {
            jobs.Remove(job);
            payloadCharge -= job.PayloadCharge;
            job.Payload = null;
        }
    }

    private static ExportJobProgress WithResult(ExportJobProgress progress, byte[]? result) => result is null
        ? progress : progress with { Result = ExportJobPayload.Deserialize<ExportResult>(result) };

    private void Signal()
    {
        if (available.CurrentCount == 0) available.Release();
    }

    // Terminal publication is atomic under gate. Running jobs reach here only
    // after the runner has returned (and reaped its child).
    private void Finish(Job job, string state, byte[]? result, string? error)
    {
        payloadCharge -= job.PayloadCharge;
        job.PayloadCharge = result?.Length ?? 0;
        payloadCharge += job.PayloadCharge;
        job.Payload = result;
        job.Error = error;
        job.State = state;
        job.Finished = clock.GetUtcNow();
        job.Cancellation?.Dispose();
        job.Cancellation = null;
    }

    private ExportJobProgress Snapshot(Job job)
    {
        var now = clock.GetUtcNow();
        var earlier = jobs.TakeWhile(j => j != job).Count(j => j.State == "queued");
        var running = jobs.Count(j => j.State == "processing");
        var queued = job.State == "queued";
        var runEstimate = Math.Max(1, (int)Math.Ceiling(averageSeconds));
        return new(job.Id, job.State, queued ? earlier + 1 : 0, queued ? earlier + running : 0,
            queued ? (earlier + running) * runEstimate : 0,
            runEstimate, (int)((job.Finished ?? now) - job.Created).TotalSeconds,
            job.Started is { } start ? (int)((job.Finished ?? now) - start).TotalSeconds : 0,
            options.ExportWorkerTimeoutSeconds, 3, Error: job.Error);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken) =>
        Task.WhenAll(Work(stoppingToken),Cleanup(stoppingToken));

    private async Task Cleanup(CancellationToken cancellation)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30), clock);
        try { while (await timer.WaitForNextTickAsync(cancellation)) { lock (gate) Prune(); } }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
    }

    private async Task Work(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await available.WaitAsync(stoppingToken); }
            catch (OperationCanceledException) { break; }
            Job? job;
            byte[] request;
            lock (gate)
            {
                job = jobs.FirstOrDefault(j => j.State == "queued");
                if (job is null) continue;
                request = job.Payload!;
                job.Payload = null;
                job.State = "processing";
                job.Started = clock.GetUtcNow();
                job.Cancellation = new();
                if (jobs.Any(j => j.State == "queued")) Signal();
            }
            byte[]? result = null;
            string? errorMessage = null;
            string state = "completed";
            try
            {
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, job.Cancellation.Token);
                var input = ExportJobPayload.Deserialize<ExportRequest>(request);
                request = [];
                result = ExportJobPayload.Serialize(await runner.RunAsync(input, linked.Token));
            }
            catch (OperationCanceledException) { state = "cancelled"; }
            catch (Exception error)
            {
                state = "failed";
                errorMessage = error is ApiFailure failure ? failure.Message : "Não foi possível concluir a exportação. Tente novamente.";
                errorMessage = errorMessage[..Math.Min(errorMessage.Length, 512)];
                logger.LogWarning("Queued export failed: {Type}", error.GetType().Name);
            }
            lock (gate)
            {
                if (stoppingToken.IsCancellationRequested || job.Cancellation!.IsCancellationRequested)
                { state = "cancelled"; errorMessage = null; }
                if (state == "completed")
                    averageSeconds = averageSeconds * 0.75 + Math.Min(options.ExportWorkerTimeoutSeconds,
                        (clock.GetUtcNow() - job.Started!.Value).TotalSeconds) * 0.25;
                Finish(job, state, state == "completed" ? result : null, errorMessage);
            }
            request = [];
            result = null;
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        lock (gate)
        {
            stopping = true;
            foreach (var job in jobs.Where(j => j.State == "queued")) Finish(job, "cancelled", null, null);
        }
        return base.StopAsync(cancellationToken);
    }
}
