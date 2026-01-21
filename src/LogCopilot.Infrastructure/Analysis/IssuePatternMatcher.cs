using LogCopilot.Application.DTOs;
using LogCopilot.Domain.Entities;

namespace LogCopilot.Infrastructure.Analysis;

public static class IssuePatternMatcher
{
    public static List<RootCauseHypothesis> AnalyzePatterns(
        List<LogEvent> events,
        List<IssueCluster> clusters
    )
    {
        var hypotheses = new List<RootCauseHypothesis>();

        foreach (var cluster in clusters.Take(5))
        {
            var clusterEvents = events
                .Where(e => e.ClusterEvents.Any(ce => ce.IssueClusterId == cluster.Id))
                .ToList();
            var hypothesis = MatchPattern(cluster, clusterEvents);
            if (hypothesis != null)
                hypotheses.Add(hypothesis);
        }

        return hypotheses;
    }

    private static RootCauseHypothesis? MatchPattern(IssueCluster cluster, List<LogEvent> events)
    {
        var title = cluster.Title.ToLower();
        var messages = events.Select(e => e.RenderedMessage.ToLower()).ToList();

        if (
            title.Contains("jwt")
            && (title.Contains("malformed") || title.Contains("not well formed"))
        )
        {
            return new RootCauseHypothesis
            {
                Issue = cluster.Title,
                LikelyCause =
                    "Invalid JWT token format - token is not properly encoded or missing required segments",
                SupportingEvidence = new List<string>
                {
                    "TokenMalformedException indicates token parsing failure",
                    "Common causes: missing Bearer prefix, wrong encoding, truncated token",
                    "May be caused by client sending malformed Authorization header",
                    $"Occurred {cluster.EventCount} times between {cluster.FirstSeen:g} and {cluster.LastSeen:g}",
                },
                Confidence = "High",
            };
        }

        if (title.Contains("token") && title.Contains("expired"))
        {
            return new RootCauseHypothesis
            {
                Issue = cluster.Title,
                LikelyCause = "JWT token lifetime exceeded - tokens are expiring before refresh",
                SupportingEvidence = new List<string>
                {
                    "Token expiration indicates session management issue",
                    "Possible causes: short token lifetime, missing refresh flow, clock skew between services",
                    "Users may experience frequent re-authentication prompts",
                    $"Affected {cluster.EventCount} requests",
                },
                Confidence = "High",
            };
        }

        if (title.Contains("validation") && title.Contains("failed"))
        {
            return new RootCauseHypothesis
            {
                Issue = cluster.Title,
                LikelyCause =
                    "Input validation failure - invalid or missing required fields in requests",
                SupportingEvidence = new List<string>
                {
                    "Validation errors suggest API contract violations",
                    "Check for: missing required fields, incorrect data types, format mismatches",
                    "May indicate outdated client code or API documentation drift",
                    $"Validation failed {cluster.EventCount} times",
                },
                Confidence = "Medium",
            };
        }

        if (title.Contains("database") || title.Contains("connection") || title.Contains("timeout"))
        {
            return new RootCauseHypothesis
            {
                Issue = cluster.Title,
                LikelyCause = "Database connectivity or performance issue",
                SupportingEvidence = new List<string>
                {
                    "Connection errors indicate infrastructure problems",
                    "Possible causes: connection pool exhaustion, network issues, database overload",
                    "Check database health, connection strings, and network connectivity",
                    $"Connection issues occurred {cluster.EventCount} times",
                },
                Confidence = "Medium",
            };
        }

        if (title.Contains("nullreference") || title.Contains("null"))
        {
            return new RootCauseHypothesis
            {
                Issue = cluster.Title,
                LikelyCause =
                    "Null reference exception - missing null checks or unexpected null values",
                SupportingEvidence = new List<string>
                {
                    "NullReferenceException indicates defensive programming gap",
                    "Review stack trace for the specific property/method causing null access",
                    "Add null checks or use null-conditional operators",
                    $"Occurred {cluster.EventCount} times",
                },
                Confidence = "Medium",
            };
        }

        return new RootCauseHypothesis
        {
            Issue = cluster.Title,
            LikelyCause = "Error pattern detected - requires investigation",
            SupportingEvidence = new List<string>
            {
                $"Error occurred {cluster.EventCount} times",
                $"First seen: {cluster.FirstSeen:g}",
                $"Last seen: {cluster.LastSeen:g}",
                "Review log details and stack traces for root cause",
            },
            Confidence = "Low",
        };
    }

    public static List<FixTask> GenerateFixPlan(
        List<RootCauseHypothesis> hypotheses,
        ReportMetrics metrics
    )
    {
        var tasks = new List<FixTask>();

        foreach (var hypothesis in hypotheses.Take(3))
        {
            var title = hypothesis.Issue.ToLower();

            if (title.Contains("jwt") && title.Contains("malformed"))
            {
                tasks.Add(
                    new FixTask
                    {
                        Priority = "P0",
                        Task = "Fix JWT token parsing and validation",
                        WhatToChange =
                            "Review authentication middleware: ensure proper Bearer token extraction, validate token format before parsing, add detailed error logging for token failures",
                        RiskImpact =
                            "High - affects all authenticated requests. Test thoroughly with valid/invalid tokens",
                        HowToVerify =
                            "1) Test with malformed tokens (should return 401 with clear message) 2) Test with valid tokens (should succeed) 3) Monitor logs for TokenMalformedException disappearance",
                    }
                );
            }
            else if (title.Contains("token") && title.Contains("expired"))
            {
                tasks.Add(
                    new FixTask
                    {
                        Priority = "P1",
                        Task = "Implement token refresh flow",
                        WhatToChange =
                            "Add refresh token endpoint, extend token lifetime to reasonable value (15-60 min), implement automatic token refresh on frontend before expiration",
                        RiskImpact = "Medium - improves UX, requires client-side changes",
                        HowToVerify =
                            "1) Verify refresh token API works 2) Test token expiration handling 3) Monitor for reduced 401 errors",
                    }
                );
            }
            else if (title.Contains("validation"))
            {
                tasks.Add(
                    new FixTask
                    {
                        Priority = "P1",
                        Task = "Fix input validation and API contracts",
                        WhatToChange =
                            "Review failing endpoints, ensure validation rules match documentation, add better error messages indicating which fields are invalid",
                        RiskImpact = "Low - improves error handling and user experience",
                        HowToVerify =
                            "Test endpoints with invalid payloads, verify clear validation error messages returned",
                    }
                );
            }
            else if (title.Contains("database") || title.Contains("connection"))
            {
                tasks.Add(
                    new FixTask
                    {
                        Priority = "P0",
                        Task = "Resolve database connectivity issues",
                        WhatToChange =
                            "Check connection strings, review connection pool settings, verify database health and network connectivity, add retry logic with exponential backoff",
                        RiskImpact = "Critical - affects data access. Coordinate with DBA/DevOps",
                        HowToVerify =
                            "Monitor database connection pool metrics, verify queries execute successfully, check network latency",
                    }
                );
            }
            else
            {
                tasks.Add(
                    new FixTask
                    {
                        Priority = "P2",
                        Task =
                            $"Investigate and fix: {hypothesis.Issue.Substring(0, Math.Min(50, hypothesis.Issue.Length))}",
                        WhatToChange =
                            "Review stack traces and error messages, identify root cause, implement fix with tests",
                        RiskImpact = "Variable - assess based on error frequency and user impact",
                        HowToVerify = "Deploy fix, monitor for error reduction in logs",
                    }
                );
            }
        }

        if (metrics.ErrorCount > 100)
        {
            tasks.Insert(
                0,
                new FixTask
                {
                    Priority = "P0",
                    Task = "Emergency error volume reduction",
                    WhatToChange =
                        "High error rate detected - prioritize most frequent errors first, consider emergency hotfix deployment",
                    RiskImpact = "Critical - system health degraded",
                    HowToVerify = "Monitor error rate dropping below 10/hour threshold",
                }
            );
        }

        return tasks;
    }

    public static List<string> DetectObservabilityGaps(List<LogEvent> events, ReportMetrics metrics)
    {
        var gaps = new List<string>();

        var tracedEvents = events.Count(e =>
            !string.IsNullOrEmpty(e.TraceId) || !string.IsNullOrEmpty(e.CorrelationId)
        );
        var tracePercentage = events.Count > 0 ? (tracedEvents * 100.0 / events.Count) : 0;

        if (tracePercentage < 10)
        {
            gaps.Add(
                "**Missing Trace Correlation**: Only "
                    + tracePercentage.ToString("F1")
                    + "% of events have trace IDs. Enable distributed tracing:\n"
                    + "- Add Activity/TraceId enrichment in logging pipeline\n"
                    + "- .NET: Use System.Diagnostics.Activity and Serilog.Enrichers.Span\n"
                    + "- Frontend: Propagate trace context in API calls (W3C Trace Context headers)"
            );
        }

        var structuredEvents = events.Count(e => !string.IsNullOrEmpty(e.PropertiesJson));
        if (structuredEvents < events.Count * 0.5)
        {
            gaps.Add(
                "**Insufficient Structured Logging**: Many events lack structured properties. Use structured logging with named parameters instead of string interpolation."
            );
        }

        var eventsWithHttp = events.Count(e => e.HttpDetails != null);
        if (
            eventsWithHttp < events.Count * 0.1
            && events.Any(e =>
                e.RenderedMessage.Contains("HTTP") || e.RenderedMessage.Contains("request")
            )
        )
        {
            gaps.Add(
                "**Limited HTTP Context**: Few events include HTTP details. Enrich logs with request path, method, status code, duration for better request correlation."
            );
        }

        var eventsWithException = events.Count(e => e.ExceptionDetails != null);
        var errorEvents = events.Count(e => e.Level >= LogLevel.Error);
        if (errorEvents > 0 && eventsWithException < errorEvents * 0.3)
        {
            gaps.Add(
                "**Missing Exception Details**: Many error events lack structured exception data. Ensure exceptions are logged with full stack traces and exception properties."
            );
        }

        return gaps;
    }

    public static string RedactSecrets(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        var redacted = System.Text.RegularExpressions.Regex.Replace(
            text,
            @"Bearer\s+[A-Za-z0-9\-_]+\.[A-Za-z0-9\-_]+\.[A-Za-z0-9\-_]+",
            "Bearer [REDACTED_JWT]"
        );

        redacted = System.Text.RegularExpressions.Regex.Replace(
            redacted,
            @"(password|pwd|secret|token|api[_-]?key)[""']?\s*[:=]\s*[""']?([^""'\s,}]+)",
            "$1=[REDACTED]",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase
        );

        redacted = System.Text.RegularExpressions.Regex.Replace(
            redacted,
            @"(Server|Data Source|Initial Catalog|User ID|Password)\s*=\s*[^;]+",
            "$1=[REDACTED]",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase
        );

        return redacted;
    }
}
