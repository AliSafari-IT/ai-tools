using LogCopilot.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LogCopilot.Infrastructure.Services;

public class QueuedHostedService : BackgroundService
{
    private readonly IBackgroundTaskQueue _taskQueue;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<QueuedHostedService> _logger;

    public QueuedHostedService(
        IBackgroundTaskQueue taskQueue,
        IServiceProvider serviceProvider,
        ILogger<QueuedHostedService> logger)
    {
        _taskQueue = taskQueue;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("QueuedHostedService.StartAsync called");
        await base.StartAsync(cancellationToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("QueuedHostedService.StopAsync called");
        await base.StopAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Queued Hosted Service is starting");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Waiting for background task...");
                var workItem = await _taskQueue.DequeueAsync(stoppingToken);

                _logger.LogInformation("Dequeued background task, executing...");

                await using var scope = _serviceProvider.CreateAsyncScope();

                try
                {
                    await workItem(scope.ServiceProvider, stoppingToken);
                    _logger.LogInformation("Background task completed successfully");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred executing background task");
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Queued Hosted Service cancellation requested");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in queued hosted service");
            }
        }

        _logger.LogInformation("Queued Hosted Service is stopping");
    }
}
