using LogCopilot.Application.DTOs;

namespace LogCopilot.Infrastructure.Integrations;

public interface IIntegrationSink
{
    Task SendReportSummaryAsync(Guid reportId, ReportOutput report, string targetUrl);
    string SinkName { get; }
}
