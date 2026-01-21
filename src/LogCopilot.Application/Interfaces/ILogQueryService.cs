using LogCopilot.Application.DTOs;

namespace LogCopilot.Application.Interfaces;

public interface ILogQueryService
{
    Task<PagedResult<LogEventDto>> GetLogsAsync(LogQueryDto query);
    Task<LogEventDetailDto> GetLogByIdAsync(Guid id);
}
