using LogCopilot.Application.DTOs;
using LogCopilot.Domain.Entities;
using LogCopilot.Infrastructure.Analysis;

namespace LogCopilot.Infrastructure.Plugins;

public class CommunityHeuristicNarrativeGenerator : IIncidentNarrativeGenerator
{
    public string ProviderName => "Community";

    public async Task<ReportOutput> GenerateNarrativeAsync(List<LogEvent> events, Domain.Entities.ReportScope scope, GenerateReportDto dto)
    {
        return await Task.FromResult(GenerateComprehensiveReport(events, scope));
    }

    private ReportOutput GenerateComprehensiveReport(List<LogEvent> events, Domain.Entities.ReportScope scope)
    {
        if (events.Count == 0)
        {
            return new ReportOutput
            {
                ExecutiveSummary = "No log events found for the specified scope.",
                Metrics = new ReportMetrics(),
                TopIssues = new List<TopIssue>(),
                RootCauseHypotheses = new List<RootCauseHypothesis>(),
                RecommendedFixPlan = new List<FixTask>(),
                ObservabilityGaps = new List<string> { "No events available for analysis" },
                TimelineHighlights = new List<TimelineHighlight>(),
                Provider = ProviderName
            };
        }

        var metrics = new ReportMetrics
        {
            TotalEvents = events.Count,
            ErrorCount = events.Count(e => e.Level >= LogLevel.Error),
            FatalCount = events.Count(e => e.Level == LogLevel.Fatal),
            WarningCount = events.Count(e => e.Level == LogLevel.Warning),
            InfoCount = events.Count(e => e.Level == LogLevel.Information || e.Level == LogLevel.Debug || e.Level == LogLevel.Verbose),
            FirstSeen = events.Any() ? events.Min(e => e.Timestamp) : DateTime.UtcNow,
            LastSeen = events.Any() ? events.Max(e => e.Timestamp) : DateTime.UtcNow
        };
        metrics.DurationHours = (metrics.LastSeen - metrics.FirstSeen).TotalHours;

        var clusterIds = events
            .SelectMany(e => e.ClusterEvents.Select(ce => ce.IssueClusterId))
            .Distinct()
            .ToList();

        var topIssues = new List<TopIssue>();
        foreach (var clusterId in clusterIds.Take(5))
        {
            var clusterEvents = events.Where(e => e.ClusterEvents.Any(ce => ce.IssueClusterId == clusterId)).ToList();
            if (clusterEvents.Any())
            {
                var evidence = clusterEvents.Take(3).Select(e => 
                    IssuePatternMatcher.RedactSecrets(e.RenderedMessage.Length > 150 
                        ? e.RenderedMessage.Substring(0, 150) + "..." 
                        : e.RenderedMessage)).ToList();

                var actions = new List<string> { "Review stack trace for root cause", "Add additional logging if needed", "Implement error handling" };

                topIssues.Add(new TopIssue
                {
                    ClusterId = clusterId,
                    Title = $"Cluster {clusterId}",
                    Severity = "Medium",
                    Count = clusterEvents.Count,
                    FirstSeen = clusterEvents.Min(e => e.Timestamp),
                    LastSeen = clusterEvents.Max(e => e.Timestamp),
                    Evidence = evidence,
                    SuggestedActions = actions
                });
            }
        }

        metrics.UniqueClusters = topIssues.Count;

        var hypotheses = IssuePatternMatcher.AnalyzePatterns(events, new List<IssueCluster>());
        var fixPlan = IssuePatternMatcher.GenerateFixPlan(hypotheses, metrics);
        var observabilityGaps = IssuePatternMatcher.DetectObservabilityGaps(events, metrics);

        var timeline = events
            .GroupBy(e => new DateTime(e.Timestamp.Year, e.Timestamp.Month, e.Timestamp.Day, e.Timestamp.Hour, 0, 0))
            .OrderBy(g => g.Key)
            .Select(g => new TimelineHighlight
            {
                Timestamp = g.Key,
                Event = $"{g.Count(e => e.Level >= LogLevel.Error)} errors, {g.Count(e => e.Level == LogLevel.Warning)} warnings",
                EventCount = g.Count()
            })
            .Where(t => t.EventCount > 5)
            .Take(10)
            .ToList();

        var healthStatus = metrics.ErrorCount > 100 ? "CRITICAL" :
                          metrics.ErrorCount > 50 ? "DEGRADED" :
                          metrics.ErrorCount > 10 ? "WARNING" : "HEALTHY";

        var executiveSummary = $"System health: {healthStatus}. Analyzed {metrics.TotalEvents} log events over {metrics.DurationHours:F1} hours ({metrics.FirstSeen:g} to {metrics.LastSeen:g}). " +
                             $"Detected {metrics.ErrorCount} errors, {metrics.FatalCount} fatal events, and {metrics.WarningCount} warnings across {metrics.UniqueClusters} distinct issue clusters. ";

        if (metrics.ErrorCount > 50)
            executiveSummary += $"HIGH PRIORITY: Error volume exceeds normal thresholds ({metrics.ErrorCount} errors detected). Immediate investigation recommended. ";

        if (topIssues.Any())
        {
            var topCluster = topIssues.First();
            executiveSummary += $"Top issue: {topCluster.Title} ({topCluster.Count} occurrences, {topCluster.Severity} severity). ";
        }

        if (observabilityGaps.Any())
            executiveSummary += $"Observability gaps detected: trace correlation is limited, affecting request flow analysis.";

        return new ReportOutput
        {
            ExecutiveSummary = executiveSummary,
            Metrics = metrics,
            TopIssues = topIssues,
            RootCauseHypotheses = hypotheses,
            RecommendedFixPlan = fixPlan,
            ObservabilityGaps = observabilityGaps,
            TimelineHighlights = timeline,
            Provider = ProviderName
        };
    }
}
