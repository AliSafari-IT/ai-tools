using LogCopilot.Application.DTOs;

namespace LogCopilot.Application.Interfaces;

public interface ITraceService
{
    Task<PagedResult<RequestTraceDto>> GetTracesAsync(TraceQueryDto query);
    Task<TraceDetailDto> GetTraceByIdAsync(Guid id);
}
