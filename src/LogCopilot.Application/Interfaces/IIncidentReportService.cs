using LogCopilot.Application.DTOs;

namespace LogCopilot.Application.Interfaces;

public interface IIncidentReportService
{
    Task<IncidentReportDto> GenerateReportAsync(GenerateReportDto dto);
    Task<PagedResult<IncidentReportDto>> GetReportsAsync(int page, int pageSize);
    Task<IncidentReportDetailDto> GetReportByIdAsync(Guid id);
}
