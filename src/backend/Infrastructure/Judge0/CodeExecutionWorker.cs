using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Judge0;

public sealed class CodeExecutionWorker(IServiceScopeFactory scopes, ILogger<CodeExecutionWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopes.CreateScope();
                var progressed = await scope.ServiceProvider.GetRequiredService<CodeExecutionProcessor>().ProcessNextAsync(stoppingToken);
                using var submissionScope = scopes.CreateScope();
                progressed |= await submissionScope.ServiceProvider.GetRequiredService<SubmissionProcessor>().ProcessNextAsync(stoppingToken);
                if (progressed)
                    continue;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { logger.LogError(ex, "Code execution worker iteration failed."); }
            try { await Task.Delay(1000, stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
        }
    }
}
