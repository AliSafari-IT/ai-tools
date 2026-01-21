using LogCopilot.Application.DTOs;

namespace LogCopilot.Application.Interfaces;

public interface IClusterService
{
    Task<PagedResult<IssueClusterDto>> GetClustersAsync(ClusterQueryDto query);
    Task<ClusterDetailDto> GetClusterByIdAsync(Guid id);
}
