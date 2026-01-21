using System.Text.Json;

namespace LogCopilot.Infrastructure.AI;

public class MockAIProvider : IAIProvider
{
    public Task<string> GenerateIncidentReportAsync(string prompt)
    {
        var mockReport = new
        {
            summary = "Mock incident report generated for testing purposes.",
            suspected_causes = new[]
            {
                "Database connection timeout",
                "High memory usage on application server"
            },
            evidence = new[]
            {
                new
                {
                    eventId = Guid.NewGuid(),
                    lineNumber = 42,
                    excerpt = "System.Data.SqlClient.SqlException: Timeout expired",
                    relevance = "Indicates database connectivity issue"
                }
            },
            impact = "Service degradation affecting approximately 15% of requests",
            recommended_actions = new[]
            {
                "Investigate database server performance metrics",
                "Review connection pool configuration",
                "Check for long-running queries"
            },
            followups = new[]
            {
                "Monitor error rates over next 24 hours",
                "Review similar incidents from past week"
            },
            confidence = "Medium"
        };

        return Task.FromResult(JsonSerializer.Serialize(mockReport));
    }
}
