using System.Security.Cryptography;
using System.Text;
using LogCopilot.Domain.Entities;
using LogCopilot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LogCopilot.Infrastructure.Clustering;

public class ClusteringService
{
    private readonly LogCopilotDbContext _context;

    public ClusteringService(LogCopilotDbContext context)
    {
        _context = context;
    }

    public async Task ProcessEventForClusteringAsync(LogEvent logEvent)
    {
        if (logEvent.ExceptionDetails != null)
        {
            await ProcessExceptionClusterAsync(logEvent);
        }
        else if (logEvent.Level >= LogLevel.Warning)
        {
            await ProcessGenericErrorClusterAsync(logEvent);
        }
        
        if (logEvent.HttpDetails != null && logEvent.HttpDetails.StatusCode >= 400)
        {
            await ProcessEndpointErrorClusterAsync(logEvent);
        }
    }

    private async Task ProcessExceptionClusterAsync(LogEvent logEvent)
    {
        var signature = logEvent.ExceptionDetails!.SignatureHash;
        
        var cluster = await _context.IssueClusters
            .FirstOrDefaultAsync(c => c.OrganizationId == logEvent.OrganizationId && c.Signature == signature);

        if (cluster == null)
        {
            cluster = new IssueCluster
            {
                Id = Guid.NewGuid(),
                OrganizationId = logEvent.OrganizationId,
                Type = ClusterType.Exception,
                Signature = signature,
                Title = $"{logEvent.ExceptionDetails.Type}: {TruncateMessage(logEvent.ExceptionDetails.Message, 100)}",
                FirstSeen = logEvent.Timestamp,
                LastSeen = logEvent.Timestamp,
                Severity = DetermineSeverity(logEvent.Level),
                Status = IssueStatus.Open,
                EventCount = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedBy = logEvent.CreatedBy,
                UpdatedBy = logEvent.UpdatedBy
            };
            _context.IssueClusters.Add(cluster);
        }
        else
        {
            cluster.LastSeen = logEvent.Timestamp;
            cluster.EventCount++;
            cluster.UpdatedAt = DateTime.UtcNow;
        }

        var clusterEvent = new ClusterEvent
        {
            Id = Guid.NewGuid(),
            IssueClusterId = cluster.Id,
            LogEventId = logEvent.Id,
            CreatedAt = DateTime.UtcNow
        };
        _context.ClusterEvents.Add(clusterEvent);

        await _context.SaveChangesAsync();
    }

    private async Task ProcessGenericErrorClusterAsync(LogEvent logEvent)
    {
        var messagePrefix = TruncateMessage(logEvent.RenderedMessage, 100);
        var signature = ComputeHash($"{logEvent.Level}:{messagePrefix}");
        
        var cluster = await _context.IssueClusters
            .FirstOrDefaultAsync(c => c.OrganizationId == logEvent.OrganizationId && c.Signature == signature);

        if (cluster == null)
        {
            cluster = new IssueCluster
            {
                Id = Guid.NewGuid(),
                OrganizationId = logEvent.OrganizationId,
                Type = ClusterType.Exception,
                Signature = signature,
                Title = TruncateMessage(logEvent.RenderedMessage, 150),
                FirstSeen = logEvent.Timestamp,
                LastSeen = logEvent.Timestamp,
                Severity = DetermineSeverity(logEvent.Level),
                Status = IssueStatus.Open,
                EventCount = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedBy = logEvent.CreatedBy,
                UpdatedBy = logEvent.UpdatedBy
            };
            _context.IssueClusters.Add(cluster);
        }
        else
        {
            cluster.LastSeen = logEvent.Timestamp;
            cluster.EventCount++;
            cluster.UpdatedAt = DateTime.UtcNow;
        }

        var clusterEvent = new ClusterEvent
        {
            Id = Guid.NewGuid(),
            IssueClusterId = cluster.Id,
            LogEventId = logEvent.Id,
            CreatedAt = DateTime.UtcNow
        };
        _context.ClusterEvents.Add(clusterEvent);

        await _context.SaveChangesAsync();
    }

    private async Task ProcessEndpointErrorClusterAsync(LogEvent logEvent)
    {
        var endpointKey = $"{logEvent.HttpDetails!.Method}:{logEvent.HttpDetails.Path}:{logEvent.HttpDetails.StatusCode}";
        var signature = ComputeHash(endpointKey);
        
        var cluster = await _context.IssueClusters
            .FirstOrDefaultAsync(c => c.OrganizationId == logEvent.OrganizationId && c.Signature == signature);

        if (cluster == null)
        {
            cluster = new IssueCluster
            {
                Id = Guid.NewGuid(),
                OrganizationId = logEvent.OrganizationId,
                Type = ClusterType.EndpointError,
                Signature = signature,
                Title = $"{logEvent.HttpDetails.Method} {logEvent.HttpDetails.Path} - {logEvent.HttpDetails.StatusCode}",
                FirstSeen = logEvent.Timestamp,
                LastSeen = logEvent.Timestamp,
                Severity = DetermineSeverityFromStatus(logEvent.HttpDetails.StatusCode ?? 500),
                Status = IssueStatus.Open,
                EventCount = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedBy = logEvent.CreatedBy,
                UpdatedBy = logEvent.UpdatedBy
            };
            _context.IssueClusters.Add(cluster);
        }
        else
        {
            cluster.LastSeen = logEvent.Timestamp;
            cluster.EventCount++;
            cluster.UpdatedAt = DateTime.UtcNow;
        }

        var clusterEvent = new ClusterEvent
        {
            Id = Guid.NewGuid(),
            IssueClusterId = cluster.Id,
            LogEventId = logEvent.Id,
            CreatedAt = DateTime.UtcNow
        };
        _context.ClusterEvents.Add(clusterEvent);

        await _context.SaveChangesAsync();
    }

    public static string ComputeHash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLower();
    }

    public static string ComputeExceptionSignature(string exceptionType, string? stackTrace)
    {
        var topFrames = ExtractTopStackFrames(stackTrace, 3);
        var signature = $"{exceptionType}:{topFrames}";
        return ComputeHash(signature);
    }

    private static string ExtractTopStackFrames(string? stackTrace, int count)
    {
        if (string.IsNullOrWhiteSpace(stackTrace))
            return "";

        var lines = stackTrace.Split('\n')
            .Take(count)
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line));

        return string.Join("|", lines);
    }

    private static IssueSeverity DetermineSeverity(LogLevel level)
    {
        return level switch
        {
            LogLevel.Fatal => IssueSeverity.Critical,
            LogLevel.Error => IssueSeverity.High,
            LogLevel.Warning => IssueSeverity.Medium,
            _ => IssueSeverity.Low
        };
    }

    private static IssueSeverity DetermineSeverityFromStatus(int statusCode)
    {
        return statusCode switch
        {
            >= 500 => IssueSeverity.High,
            >= 400 => IssueSeverity.Medium,
            _ => IssueSeverity.Low
        };
    }

    private static string TruncateMessage(string message, int maxLength)
    {
        if (message.Length <= maxLength)
            return message;
        return message[..maxLength] + "...";
    }
}
