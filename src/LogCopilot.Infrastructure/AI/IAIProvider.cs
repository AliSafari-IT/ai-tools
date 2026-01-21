namespace LogCopilot.Infrastructure.AI;

public interface IAIProvider
{
    Task<string> GenerateIncidentReportAsync(string prompt);
}
