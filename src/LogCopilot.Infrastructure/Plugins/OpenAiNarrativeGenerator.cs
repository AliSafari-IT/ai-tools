using System.Text.Json;
using LogCopilot.Application.DTOs;
using LogCopilot.Domain.Entities;
using LogCopilot.Infrastructure.AI;
using Microsoft.Extensions.Logging;

namespace LogCopilot.Infrastructure.Plugins;

public class OpenAiNarrativeGenerator : IIncidentNarrativeGenerator
{
    private readonly IAIProvider _aiProvider;
    private readonly ILogger<OpenAiNarrativeGenerator> _logger;
    private readonly CommunityHeuristicNarrativeGenerator _fallback;

    public string ProviderName => "OpenAI";

    public OpenAiNarrativeGenerator(
        IAIProvider aiProvider,
        ILogger<OpenAiNarrativeGenerator> logger
    )
    {
        _aiProvider = aiProvider;
        _logger = logger;
        _fallback = new CommunityHeuristicNarrativeGenerator();
    }

    public async Task<ReportOutput> GenerateNarrativeAsync(
        List<LogEvent> events,
        Domain.Entities.ReportScope scope,
        GenerateReportDto dto
    )
    {
        if (events.Count == 0)
        {
            return await _fallback.GenerateNarrativeAsync(events, scope, dto);
        }

        try
        {
            var errorCount = events.Count(e => e.Level >= Domain.Entities.LogLevel.Error);
            var warningCount = events.Count(e => e.Level == Domain.Entities.LogLevel.Warning);
            var prompt = BuildReportPrompt(events, errorCount, warningCount, scope);
            var aiResponse = await _aiProvider.GenerateIncidentReportAsync(prompt);

            try
            {
                var output = JsonSerializer.Deserialize<ReportOutput>(aiResponse);
                if (output != null)
                {
                    output.Provider = ProviderName;
                    return output;
                }
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to parse AI response as JSON, using fallback");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI provider failed, using fallback report generation");
        }

        return await _fallback.GenerateNarrativeAsync(events, scope, dto);
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

Generate a JSON object with this structure:
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
