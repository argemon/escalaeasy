using EscalaLivre.Api;
using EscalaLivre.Application.Contracts;
using Microsoft.Extensions.Logging.Abstractions;

namespace EscalaLivre.Api.Tests;

public sealed class ExportJobQueueTests
{
    private static ExportRequest Request()=>new(ScheduleJobQueueTests.Request().Document,"pdf");
    private sealed class Clock:TimeProvider
    {
        public DateTimeOffset Now=DateTimeOffset.UnixEpoch;
        public override DateTimeOffset GetUtcNow()=>Now;
    }
    private sealed class Runner(Func<ExportRequest,CancellationToken,Task<ExportResult>> run):IExportRunner
    {
        public Task<ExportResult> RunAsync(ExportRequest request,CancellationToken cancellation)=>run(request,cancellation);
    }
    private static async Task Until(Func<bool> ready)
    {
        using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while(!ready())await Task.Delay(5,deadline.Token);
    }
    [Fact]
    public async Task FifoReportsRealPositionsWaitsBeyondFiveSecondsAndCancelsWithoutParallelism()
    {
        int active=0,maximum=0;
        var clock=new Clock();var entered=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var runner=new Runner(async(_,c)=>{
            maximum=Math.Max(maximum,Interlocked.Increment(ref active));entered.TrySetResult();
            try{await Task.Delay(Timeout.Infinite,c);return new("a.pdf","","application/pdf");}
            finally{Interlocked.Decrement(ref active);}
        });
        using var queue=new ExportJobQueue(runner,new(){ExportJobQueueLimit=2},NullLogger<ExportJobQueue>.Instance,clock);
        await queue.StartAsync(default);
        try{
            var first=queue.Enqueue(Request());await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
            var second=queue.Enqueue(Request());var third=queue.Enqueue(Request());
            Assert.Equal(1,second.Position);Assert.Equal(1,second.JobsAhead);Assert.Equal(2,third.Position);Assert.Equal(2,third.JobsAhead);
            Assert.Equal(503,Assert.Throws<ApiFailure>(()=>queue.Enqueue(Request())).Status);
            clock.Now=clock.Now.AddSeconds(30);
            Assert.Equal("queued",queue.Get(second.Id).State);Assert.Equal(30,queue.Get(second.Id).ElapsedSeconds);
            Assert.Equal("cancelled",queue.Cancel(second.Id).State);Assert.Equal(1,queue.Get(third.Id).Position);
            queue.Cancel(first.Id);await Until(()=>queue.Get(third.Id).State=="processing");
            Assert.Equal("cancelled",queue.Get(first.Id).State);Assert.Equal(1,maximum);
            Assert.Equal(404,Assert.Throws<ApiFailure>(()=>queue.Get(new string('0',64))).Status);
        }finally{await queue.StopAsync(default);}
        Assert.Equal(0,active);Assert.Equal(0,queue.RetentionUsage.PayloadBytes);
    }
    [Fact]
    public async Task RetentionBudgetExpiryAndLegacyRequestsUseTheSameQueue()
    {
        var clock=new Clock();
        using var queue=new ExportJobQueue(new Runner((_,_)=>Task.FromResult(new ExportResult("a.pdf","JVBERi0=","application/pdf"))),
            new(){ExportJobPayloadBudgetMb=16},NullLogger<ExportJobQueue>.Instance,clock);
        await queue.StartAsync(default);
        try{
            var first=queue.Enqueue(Request());await Until(()=>queue.Get(first.Id).State=="completed");
            Assert.Equal("JVBERi0=",queue.Get(first.Id).Result!.ContentBase64);
            Assert.NotSame(queue.Get(first.Id).Result,queue.Get(first.Id).Result);
            Assert.Equal("queueFull",Assert.Throws<ApiFailure>(()=>queue.Enqueue(Request())).Code);
            clock.Now=clock.Now.AddSeconds(119);Assert.Equal("completed",queue.Get(first.Id).State);
            clock.Now=clock.Now.AddSeconds(1);Assert.Equal(404,Assert.Throws<ApiFailure>(()=>queue.Get(first.Id)).Status);
            Assert.Equal("a.pdf",(await queue.RunLegacyAsync(Request(),default)).FileName);
        }finally{await queue.StopAsync(default);}
    }
    [Fact]
    public async Task OversizedResultFailsAndDoesNotBlockFollowingExports()
    {
        int count=0;
        using var queue=new ExportJobQueue(new Runner((_,_)=>Task.FromResult(new ExportResult("a.pdf",
            Interlocked.Increment(ref count)==1?new string('A',ExportJobPayload.MaximumBytes):"JVBERi0=","application/pdf"))),new(),NullLogger<ExportJobQueue>.Instance);
        await queue.StartAsync(default);
        try{
            var first=queue.Enqueue(Request());await Until(()=>queue.Get(first.Id).State=="failed");
            var second=queue.Enqueue(Request());await Until(()=>queue.Get(second.Id).State=="completed");
            Assert.NotNull(queue.Get(first.Id).Error);Assert.NotNull(queue.Get(second.Id).Result);
        }finally{await queue.StopAsync(default);}
    }
    [Theory]
    [InlineData("native")]
    [InlineData("exe")]
    public void RejectsInvalidQueuedFormats(string format)
    {
        using var queue=new ExportJobQueue(new Runner((_,_)=>throw new Exception()),new(),NullLogger<ExportJobQueue>.Instance);
        Assert.Equal(400,Assert.Throws<ApiFailure>(()=>queue.Enqueue(Request() with{Format=format})).Status);
        Assert.Equal((0,0L),queue.RetentionUsage);
    }
    [Theory]
    [InlineData(0,20,15,512,384,128)]
    [InlineData(6,21,15,512,384,128)]
    [InlineData(6,20,0,512,384,128)]
    [InlineData(6,20,15,0,384,128)]
    [InlineData(6,20,15,512,15,128)]
    [InlineData(6,20,15,512,384,0)]
    public void RejectsUnsafeOptions(int rate,int waiting,int seconds,int memory,int budget,int retention)=>
        Assert.Throws<InvalidOperationException>(()=>new ProtectionOptions{ExportsPerMinute=rate,ExportJobQueueLimit=waiting,
            ExportWorkerTimeoutSeconds=seconds,ExportWorkerMemoryMb=memory,ExportJobPayloadBudgetMb=budget,ExportJobRetentionLimit=retention}.Validate());
}
