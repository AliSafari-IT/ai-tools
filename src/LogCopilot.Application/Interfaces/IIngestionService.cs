namespace LogCopilot.Application.Interfaces;

public interface IIngestionService
{
    Task ProcessIngestionJobAsync(Guid jobId);
}
