using LogCopilot.Application.DTOs;
using LogCopilot.Domain.Entities;

namespace LogCopilot.Infrastructure.Plugins;

public interface IIncidentNarrativeGenerator
{
    Task<ReportOutput> GenerateNarrativeAsync(
        List<LogEvent> events,
        Domain.Entities.ReportScope scope,
        GenerateReportDto dto
    );
    string ProviderName { get; }
}
