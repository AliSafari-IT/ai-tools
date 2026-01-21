using LogCopilot.Application.DTOs;
using Microsoft.Extensions.Logging;

namespace LogCopilot.Infrastructure.Integrations;

public class WebhookIntegrationSink : IIntegrationSink
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WebhookIntegrationSink> _logger;

    public string SinkName => "Webhook";

    public WebhookIntegrationSink(HttpClient httpClient, ILogger<WebhookIntegrationSink> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task SendReportSummaryAsync(Guid reportId, ReportOutput report, string targetUrl)
    {
        try
        {
            var payload = new
            {
                reportId,
                summary = report.ExecutiveSummary,
                provider = report.Provider,
                timestamp = DateTime.UtcNow
            };

            var content = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(payload),
                System.Text.Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(targetUrl, content);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send webhook integration for report {ReportId}", reportId);
        }
    }
}
