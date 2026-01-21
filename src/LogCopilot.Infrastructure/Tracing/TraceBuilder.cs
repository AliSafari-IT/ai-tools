using LogCopilot.Domain.Entities;
using LogCopilot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LogCopilot.Infrastructure.Tracing;

public class TraceBuilder
{
    private readonly LogCopilotDbContext _context;

    public TraceBuilder(LogCopilotDbContext context)
    {
        _context = context;
    }

    public async Task ProcessEventForTracingAsync(LogEvent logEvent)
    {
        var traceKey = logEvent.TraceId ?? logEvent.CorrelationId ?? logEvent.RequestId;
        if (string.IsNullOrEmpty(traceKey))
            return;

        var trace = await _context.RequestTraces
            .Include(t => t.TraceEvents)
            .FirstOrDefaultAsync(t => 
                t.OrganizationId == logEvent.OrganizationId &&
                (t.TraceId == logEvent.TraceId || 
                 t.CorrelationId == logEvent.CorrelationId || 
                 t.RequestId == logEvent.RequestId));

        if (trace == null)
        {
            trace = new RequestTrace
            {
                Id = Guid.NewGuid(),
                OrganizationId = logEvent.OrganizationId,
                TraceId = logEvent.TraceId,
                CorrelationId = logEvent.CorrelationId,
                RequestId = logEvent.RequestId,
                StartTime = logEvent.Timestamp,
                EndTime = logEvent.Timestamp,
                DurationMs = 0,
                EventCount = 1,
                ErrorCount = logEvent.Level >= LogLevel.Error ? 1 : 0,
                WarningCount = logEvent.Level == LogLevel.Warning ? 1 : 0,
                SpanCount = !string.IsNullOrEmpty(logEvent.SpanId) ? 1 : 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedBy = logEvent.CreatedBy,
                UpdatedBy = logEvent.UpdatedBy
            };
            _context.RequestTraces.Add(trace);
        }
        else
        {
            if (logEvent.Timestamp < trace.StartTime)
                trace.StartTime = logEvent.Timestamp;
            
            if (logEvent.Timestamp > trace.EndTime)
                trace.EndTime = logEvent.Timestamp;

            trace.DurationMs = (trace.EndTime - trace.StartTime).TotalMilliseconds;
            trace.EventCount++;
            
            if (logEvent.Level >= LogLevel.Error)
                trace.ErrorCount++;
            
            if (logEvent.Level == LogLevel.Warning)
                trace.WarningCount++;
            
            if (!string.IsNullOrEmpty(logEvent.SpanId))
                trace.SpanCount++;

            trace.UpdatedAt = DateTime.UtcNow;
        }

        var ordinal = trace.TraceEvents.Count;
        var traceEvent = new TraceEvent
        {
            Id = Guid.NewGuid(),
            RequestTraceId = trace.Id,
            LogEventId = logEvent.Id,
            Ordinal = ordinal,
            CreatedAt = DateTime.UtcNow
        };
        _context.TraceEvents.Add(traceEvent);

        await _context.SaveChangesAsync();
    }
}
