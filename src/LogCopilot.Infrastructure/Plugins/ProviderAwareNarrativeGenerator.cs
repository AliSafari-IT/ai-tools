using System.Text.Json;
using LogCopilot.Application.DTOs;
using LogCopilot.Application.Interfaces;
using LogCopilot.Domain.Entities;
using LogCopilot.Infrastructure.AI;
using Microsoft.Extensions.Logging;

namespace LogCopilot.Infrastructure.Plugins;

public class ProviderAwareNarrativeGenerator : IIncidentNarrativeGenerator
{
    private readonly IAIProvider _aiProvider;
    private readonly IBillingService _billingService;
    private readonly ILogger<ProviderAwareNarrativeGenerator> _logger;
    private readonly CommunityHeuristicNarrativeGenerator _fallback;

    public string ProviderName => "OpenAI";

    public ProviderAwareNarrativeGenerator(
        IAIProvider aiProvider,
        IBillingService billingService,
        ILogger<ProviderAwareNarrativeGenerator> logger
    )
    {
        _aiProvider = aiProvider;
        _billingService = billingService;
        _logger = logger;
        _fallback = new CommunityHeuristicNarrativeGenerator();
    }

    public async Task<(
        ReportOutput output,
        string requestedProvider,
        string actualProvider,
        string providerStatus,
        string? errorSummary
    )> GenerateNarrativeWithTrackingAsync(
        List<LogEvent> events,
        ReportScope scope,
        GenerateReportDto dto,
        Guid organizationId
    )
    {
        if (events.Count == 0)
        {
            var fallbackOutput = await _fallback.GenerateNarrativeAsync(events, scope, dto);
            return (fallbackOutput, "Community", "Community", "Success", null);
        }

        var hasAiFeature = await _billingService.HasFeatureAsync(organizationId, "ai_reports");

        if (!hasAiFeature)
        {
            _logger.LogInformation(
                "Organization {OrgId} does not have AI reports feature, using Community provider",
                organizationId
            );
            var fallbackOutput = await _fallback.GenerateNarrativeAsync(events, scope, dto);
            return (fallbackOutput, "Community", "Community", "Success", null);
        }

        try
        {
            var errorCount = events.Count(e => e.Level >= Domain.Entities.LogLevel.Error);
            var warningCount = events.Count(e => e.Level == Domain.Entities.LogLevel.Warning);
            var prompt = BuildReportPrompt(events, errorCount, warningCount, scope);

            _logger.LogInformation(
                "Requesting AI report generation from OpenAI for organization {OrgId}",
                organizationId
            );
            var aiResponse = await _aiProvider.GenerateIncidentReportAsync(prompt);

            try
            {
                var output = JsonSerializer.Deserialize<ReportOutput>(
                    aiResponse,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                if (output != null)
                {
                    output.Provider = ProviderName;
                    output.RequestedProvider = "OpenAI";
                    output.ActualProvider = "OpenAI";
                    output.ProviderStatus = "Success";
                    output.ProviderErrorSummary = null;
                    _logger.LogInformation("Successfully generated AI report using OpenAI");
                    return (output, "OpenAI", "OpenAI", "Success", null);
                }
                else
                {
                    _logger.LogWarning("AI response deserialized to null, using fallback");
                    var fallbackOutput = await _fallback.GenerateNarrativeAsync(events, scope, dto);
                    return (
                        fallbackOutput,
                        "OpenAI",
                        "Community",
                        "Fallback",
                        "Deserialization returned null"
                    );
                }
            }
            catch (JsonException ex)
            {
                var errorMsg = $"JSON parsing failed: {ex.Message}";
                _logger.LogWarning(
                    ex,
                    "Failed to parse AI response as JSON, using fallback. Response length: {Length}",
                    aiResponse?.Length ?? 0
                );
                var fallbackOutput = await _fallback.GenerateNarrativeAsync(events, scope, dto);
                return (fallbackOutput, "OpenAI", "Community", "Fallback", errorMsg);
            }
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("API key"))
        {
            var errorMsg = "API key not configured";
            _logger.LogWarning("AI API key not configured, using fallback");
            var fallbackOutput = await _fallback.GenerateNarrativeAsync(events, scope, dto);
            return (fallbackOutput, "OpenAI", "Community", "Fallback", errorMsg);
        }
        catch (Exception ex)
        {
            var errorMsg = $"Provider error: {ex.GetType().Name}";
            _logger.LogError(ex, "AI provider failed, using fallback report generation");
            var fallbackOutput = await _fallback.GenerateNarrativeAsync(events, scope, dto);
            return (fallbackOutput, "OpenAI", "Community", "Fallback", errorMsg);
        }
    }

    public async Task<ReportOutput> GenerateNarrativeAsync(
        List<LogEvent> events,
        ReportScope scope,
        GenerateReportDto dto
    )
    {
        var (output, _, _, _, _) = await GenerateNarrativeWithTrackingAsync(
            events,
            scope,
            dto,
            Guid.Empty
        );
        return output;
    }

    private string BuildReportPrompt(
        List<LogEvent> events,
        int errorCount,
        int warningCount,
        ReportScope scope
    )
    {
        var logSample = string.Join(
            "\n",
            events
                .Take(20)
                .Select(e => $"[{e.Timestamp:yyyy-MM-dd HH:mm:ss}] [{e.Level}] {e.RenderedMessage}")
        );

        return $@"Analyze the following log events and generate an incident report in JSON format.

Total Events: {events.Count}
Errors: {errorCount}
Warnings: {warningCount}
Scope: {scope}

Log Sample:
{logSample}

Generate a JSON object with this exact structure:
{{
  ""ExecutiveSummary"": ""Brief executive summary"",
  ""Metrics"": {{
    ""TotalEvents"": {events.Count},
    ""ErrorCount"": {errorCount},
    ""WarningCount"": {warningCount},
    ""FatalCount"": 0,
    ""InfoCount"": {events.Count - errorCount - warningCount},
    ""UniqueClusters"": 1,
    ""FirstSeen"": ""{events.Min(e => e.Timestamp):O}"",
    ""LastSeen"": ""{events.Max(e => e.Timestamp):O}"",
    ""DurationHours"": 1.0
  }},
  ""TopIssues"": [],
  ""RootCauseHypotheses"": [],
  ""RecommendedFixPlan"": [],
  ""ObservabilityGaps"": [],
  ""TimelineHighlights"": [],
  ""Provider"": ""OpenAI""
}}";
    }
}
