namespace LogCopilot.Application.Interfaces;

public interface IIngestionService
{
    Task ProcessIngestionJobAsync(Guid jobId, CancellationToken cancellationToken = default);
    Task CancelIngestionJobAsync(Guid jobId);
}
