using LogCopilot.Application.DTOs;

namespace LogCopilot.Infrastructure.Integrations;

public class NullIntegrationSink : IIntegrationSink
{
    public string SinkName => "Null";

    public Task SendReportSummaryAsync(Guid reportId, ReportOutput report, string targetUrl)
    {
        return Task.CompletedTask;
    }
}
