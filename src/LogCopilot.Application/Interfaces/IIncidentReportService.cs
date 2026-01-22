using LogCopilot.Application.DTOs;

namespace LogCopilot.Application.Interfaces;

public interface IIncidentReportService
{
    Task<IncidentReportDto> GenerateReportAsync(GenerateReportDto dto);
    Task<PagedResult<IncidentReportDto>> GetReportsAsync(int page, int pageSize);
    Task<IncidentReportDetailDto> GetReportByIdAsync(Guid id);
    Task DeleteReportAsync(Guid id);
    Task<BulkDeleteResult> BulkDeleteReportsAsync(Guid organizationId, Guid[] ids);
}

public class BulkDeleteResult
{
    public int DeletedCount { get; set; }
    public Guid[] NotFoundIds { get; set; } = Array.Empty<Guid>();
}
