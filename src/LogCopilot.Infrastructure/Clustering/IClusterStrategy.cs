using LogCopilot.Domain.Entities;

namespace LogCopilot.Infrastructure.Clustering;

public interface IClusterStrategy
{
    Task<List<IssueCluster>> BuildClustersAsync(List<LogEvent> events);
    string StrategyName { get; }
}
