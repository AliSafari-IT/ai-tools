using LogCopilot.Application.DTOs;
using LogCopilot.Application.Interfaces;
using LogCopilot.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace LogCopilot.Infrastructure.Services;

public class TraceService : ITraceService
{
    private readonly LogCopilotDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TraceService(LogCopilotDbContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<PagedResult<RequestTraceDto>> GetTracesAsync(TraceQueryDto query)
    {
        var organizationId = GetOrganizationId();

        var queryable = _context.RequestTraces
            .Where(t => t.OrganizationId == organizationId);

        if (query.StartTime.HasValue)
        {
            queryable = queryable.Where(t => t.StartTime >= query.StartTime.Value);
        }

        if (query.EndTime.HasValue)
        {
            queryable = queryable.Where(t => t.EndTime <= query.EndTime.Value);
        }

        if (query.MinErrorCount.HasValue)
        {
            queryable = queryable.Where(t => t.ErrorCount >= query.MinErrorCount.Value);
        }

        var totalCount = await queryable.CountAsync();

        var traces = await queryable
            .OrderByDescending(t => t.StartTime)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(t => new RequestTraceDto
            {
                Id = t.Id,
                TraceId = t.TraceId,
                CorrelationId = t.CorrelationId,
                StartTime = t.StartTime,
                EndTime = t.EndTime,
                DurationMs = t.DurationMs,
                EventCount = t.EventCount,
                ErrorCount = t.ErrorCount,
                WarningCount = t.WarningCount
            })
            .ToListAsync();

        return new PagedResult<RequestTraceDto>
        {
            Items = traces,
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }

    public async Task<TraceDetailDto> GetTraceByIdAsync(Guid id)
    {
        var organizationId = GetOrganizationId();

        var trace = await _context.RequestTraces
            .Include(t => t.TraceEvents)
                .ThenInclude(te => te.LogEvent)
            .Where(t => t.Id == id && t.OrganizationId == organizationId)
            .FirstOrDefaultAsync();

        if (trace == null)
            throw new Exception("Trace not found");

        var events = trace.TraceEvents
            .OrderBy(te => te.Ordinal)
            .Select(te => new TraceEventItemDto
            {
                LogEventId = te.LogEventId,
                Ordinal = te.Ordinal,
                Timestamp = te.LogEvent.Timestamp,
                Level = te.LogEvent.Level.ToString(),
                RenderedMessage = te.LogEvent.RenderedMessage,
                SpanId = te.LogEvent.SpanId,
                DurationMs = null
            })
            .ToList();

        return new TraceDetailDto
        {
            Id = trace.Id,
            TraceId = trace.TraceId,
            CorrelationId = trace.CorrelationId,
            StartTime = trace.StartTime,
            EndTime = trace.EndTime,
            DurationMs = trace.DurationMs,
            EventCount = trace.EventCount,
            ErrorCount = trace.ErrorCount,
            WarningCount = trace.WarningCount,
            SpanCount = trace.SpanCount,
            Events = events
        };
    }

    private Guid GetOrganizationId()
    {
        var orgIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst("OrganizationId")?.Value;
        if (string.IsNullOrEmpty(orgIdClaim))
            throw new UnauthorizedAccessException("Organization ID not found");
        return Guid.Parse(orgIdClaim);
    }
}
