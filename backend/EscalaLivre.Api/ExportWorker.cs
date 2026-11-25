using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using EscalaLivre.Application.Contracts;
using EscalaLivre.Infrastructure.ImportExport;
using EscalaLivre.Infrastructure.Solver;

namespace EscalaLivre.Api;

// Only the export queue invokes this runner. Its slot remains held until the child exits.
public sealed class ExportWorker(ProtectionOptions options, IHostApplicationLifetime lifetime,
    ILogger<ExportWorker> logger) : IExportRunner
{
    private static readonly JsonSerializerOptions JsonOptions=ScheduleJson.CreateOptions();

    public async Task<ExportResult> RunAsync(ExportRequest request,CancellationToken cancellation)
    {
        using var shutdown=CancellationTokenSource.CreateLinkedTokenSource(cancellation,lifetime.ApplicationStopping);
        using var deadline=CancellationTokenSource.CreateLinkedTokenSource(shutdown.Token);
        deadline.CancelAfter(TimeSpan.FromSeconds(options.ExportWorkerTimeoutSeconds));
        var start=new ProcessStartInfo(Environment.ProcessPath!)
        {
            UseShellExecute=false,CreateNoWindow=true,
            RedirectStandardInput=true,RedirectStandardOutput=true,RedirectStandardError=true
        };
        if(string.Equals(Path.GetFileNameWithoutExtension(Environment.ProcessPath),"dotnet",StringComparison.OrdinalIgnoreCase))
            start.ArgumentList.Add(Assembly.GetExecutingAssembly().Location);
        start.ArgumentList.Add("--export-worker");
        start.Environment["DOTNET_PROCESSOR_COUNT"]="1";
        using var process=new Process{StartInfo=start};
        Task<string>? output=null,errors=null;
        try
        {
            process.Start();
            output=ReadBounded(process.StandardOutput,ExportJobPayload.MaximumBytes,deadline.Token);
            errors=ReadBounded(process.StandardError,64*1024,deadline.Token);
            await process.StandardInput.WriteLineAsync(JsonSerializer.Serialize(request,JsonOptions).AsMemory(),deadline.Token);
            process.StandardInput.Close();
            while(!process.HasExited)
            {
                deadline.Token.ThrowIfCancellationRequested();
                var memory=ProcessMemorySample.Read(()=>{process.Refresh();return process.WorkingSet64;},()=>process.HasExited);
                if(memory is null)break;
                if(memory.Value>options.ExportWorkerMemoryMb*1024L*1024)
                    throw new ApiFailure(422,"exportMemoryLimit","A exportação excedeu o limite de memória. Reduza o período ou a equipe.");
                if(output.IsFaulted)await output;
                if(errors.IsFaulted)await errors;
                await Task.Delay(50,deadline.Token);
            }
            await errors;
            var json=await output;
            if(process.ExitCode==2)throw new ApiFailure(422,"exportSizeLimit","O arquivo exportado excede o limite de 10 MB. Reduza o período ou a equipe.");
            if(process.ExitCode!=0)throw new ApiFailure(422,"exportFailed","Não foi possível preparar o arquivo para exportação.");
            return JsonSerializer.Deserialize<ExportResult>(json,JsonOptions)
                ??throw new ApiFailure(500,"exportFailed","O servidor não retornou o arquivo.");
        }
        catch(OperationCanceledException) when(!shutdown.IsCancellationRequested)
        {throw new ApiFailure(504,"exportTimeout","A exportação atingiu o tempo limite. Reduza o período ou a equipe.");}
        finally
        {
            try{if(!process.HasExited)process.Kill(entireProcessTree:true);await process.WaitForExitAsync(CancellationToken.None);}
            catch(InvalidOperationException){ }
            deadline.Cancel();
            foreach(var task in new[]{output,errors})if(task is not null){try{await task;}catch{ }}
            logger.LogInformation("Export process finished");
        }
    }

    private static async Task<string> ReadBounded(StreamReader reader,int limit,CancellationToken cancellation)
    {
        var result=new System.Text.StringBuilder();var buffer=new char[4096];int count;
        while((count=await reader.ReadAsync(buffer.AsMemory(),cancellation))>0)
        {
            if(result.Length+count>limit)throw new ApiFailure(422,"exportSizeLimit","O resultado da exportação excedeu o limite permitido.");
            result.Append(buffer,0,count);
        }
        return result.ToString();
    }

    public static async Task<int> ExecuteAsync()
    {
        try
        {
            var request=JsonSerializer.Deserialize<ExportRequest>((await Console.In.ReadLineAsync())!,JsonOptions);
            ExportJobPayload.Validate(request!);
            var result=new NativeScheduleSerializer().Export(request!);
            if(result.ContentBase64.Length>4*((ExportJobPayload.MaximumFileBytes+2)/3)
                ||Convert.FromBase64String(result.ContentBase64).Length>ExportJobPayload.MaximumFileBytes)return 2;
            await Console.Out.WriteAsync(JsonSerializer.Serialize(result,JsonOptions));
            return 0;
        }
        catch{return 1;}
    }
}
