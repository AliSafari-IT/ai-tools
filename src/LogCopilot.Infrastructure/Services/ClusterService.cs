using LogCopilot.Application.DTOs;
using LogCopilot.Application.Interfaces;
using LogCopilot.Domain.Entities;
using LogCopilot.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace LogCopilot.Infrastructure.Services;

public class ClusterService : IClusterService
{
    private readonly LogCopilotDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ClusterService(LogCopilotDbContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<PagedResult<IssueClusterDto>> GetClustersAsync(ClusterQueryDto query)
    {
        var organizationId = GetOrganizationId();

        var queryable = _context.IssueClusters
            .Where(c => c.OrganizationId == organizationId);

        if (!string.IsNullOrEmpty(query.Status))
        {
            if (Enum.TryParse<IssueStatus>(query.Status, true, out var status))
            {
                queryable = queryable.Where(c => c.Status == status);
            }
        }

        if (!string.IsNullOrEmpty(query.Severity))
        {
            if (Enum.TryParse<IssueSeverity>(query.Severity, true, out var severity))
            {
                queryable = queryable.Where(c => c.Severity == severity);
            }
        }

        var totalCount = await queryable.CountAsync();

        var clusters = await queryable
            .OrderByDescending(c => c.LastSeen)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(c => new IssueClusterDto
            {
                Id = c.Id,
                Type = c.Type.ToString(),
                Title = c.Title,
                FirstSeen = c.FirstSeen,
                LastSeen = c.LastSeen,
                Severity = c.Severity.ToString(),
                Status = c.Status.ToString(),
                EventCount = c.EventCount
            })
            .ToListAsync();

        return new PagedResult<IssueClusterDto>
        {
            Items = clusters,
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }

    public async Task<ClusterDetailDto> GetClusterByIdAsync(Guid id)
    {
        var organizationId = GetOrganizationId();

        var cluster = await _context.IssueClusters
            .Where(c => c.Id == id && c.OrganizationId == organizationId)
            .Select(c => new ClusterDetailDto
            {
                Id = c.Id,
                Type = c.Type.ToString(),
                Signature = c.Signature,
                Title = c.Title,
                FirstSeen = c.FirstSeen,
                LastSeen = c.LastSeen,
                Severity = c.Severity.ToString(),
                Status = c.Status.ToString(),
                EventCount = c.EventCount,
                RecentEvents = c.ClusterEvents
                    .OrderByDescending(ce => ce.LogEvent.Timestamp)
                    .Take(10)
                    .Select(ce => new LogEventDto
                    {
                        Id = ce.LogEvent.Id,
                        Timestamp = ce.LogEvent.Timestamp,
                        Level = ce.LogEvent.Level.ToString(),
                        RenderedMessage = ce.LogEvent.RenderedMessage,
                        TraceId = ce.LogEvent.TraceId,
                        Method = ce.LogEvent.HttpDetails != null ? ce.LogEvent.HttpDetails.Method : null,
                        Path = ce.LogEvent.HttpDetails != null ? ce.LogEvent.HttpDetails.Path : null,
                        StatusCode = ce.LogEvent.HttpDetails != null ? ce.LogEvent.HttpDetails.StatusCode : null,
                        DurationMs = ce.LogEvent.HttpDetails != null ? ce.LogEvent.HttpDetails.DurationMs : null
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();

        if (cluster == null)
            throw new Exception("Cluster not found");

        return cluster;
    }

    private Guid GetOrganizationId()
    {
        var orgIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst("OrganizationId")?.Value;
        if (string.IsNullOrEmpty(orgIdClaim))
            throw new UnauthorizedAccessException("Organization ID not found");
        return Guid.Parse(orgIdClaim);
    }
}
