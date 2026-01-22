using System.Text;
using LogCopilot.Application.DTOs;
using LogCopilot.Application.Interfaces;
using LogCopilot.Infrastructure.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogCopilot.Api.Controllers;

[ApiController]
[Route("api/clusters")]
[Authorize]
public class ClustersController : ControllerBase
{
    private readonly IClusterService _clusterService;
    private readonly ILogger<ClustersController> _logger;

    public ClustersController(IClusterService clusterService, ILogger<ClustersController> logger)
    {
        _clusterService = clusterService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetClusters([FromQuery] ClusterQueryDto query)
    {
        try
        {
            var result = await _clusterService.GetClustersAsync(query);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting clusters");
            return StatusCode(500, new { message = "Error retrieving clusters", error = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetClusterById(Guid id)
    {
        try
        {
            var result = await _clusterService.GetClusterByIdAsync(id);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cluster {ClusterId}", id);
            return StatusCode(500, new { message = "Error retrieving cluster", error = ex.Message });
        }
    }

    [HttpGet("{id}/ticket")]
    public async Task<IActionResult> ExportTicket(Guid id, [FromQuery] string target = "github")
    {
        try
        {
            var cluster = await _clusterService.GetClusterByIdAsync(id);

            var ticket = GenerateClusterTicket(id, cluster, target);
            var content = Encoding.UTF8.GetBytes(ticket);
            return File(content, "text/markdown", $"ticket-cluster-{id}.md");
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting ticket for cluster {ClusterId}", id);
            return StatusCode(500, new { message = "Error exporting ticket", error = ex.Message });
        }
    }

    private string GenerateClusterTicket(Guid clusterId, ClusterDetailDto cluster, string target)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"# {cluster.Type} - {cluster.Severity}");
        sb.AppendLine();
        sb.AppendLine("## Impact Summary");
        sb.AppendLine($"A {cluster.Severity.ToLower()} issue cluster has been detected affecting {cluster.EventCount} log events.");
        sb.AppendLine();
        sb.AppendLine("## Evidence");
        sb.AppendLine($"- **Cluster ID**: {clusterId}");
        sb.AppendLine($"- **Type**: {cluster.Type}");
        sb.AppendLine($"- **Severity**: {cluster.Severity}");
        sb.AppendLine($"- **Status**: {cluster.Status}");
        sb.AppendLine($"- **Event Count**: {cluster.EventCount}");
        sb.AppendLine($"- **First Seen**: {cluster.FirstSeen:O}");
        sb.AppendLine($"- **Last Seen**: {cluster.LastSeen:O}");
        sb.AppendLine();

        sb.AppendLine("## Acceptance Criteria");
        sb.AppendLine("- [ ] Root cause identified and documented");
        sb.AppendLine("- [ ] Fix implemented and tested");
        sb.AppendLine("- [ ] Event count for this cluster reduced to zero");
        sb.AppendLine("- [ ] No regressions in related functionality");
        sb.AppendLine();

        sb.AppendLine("## How to Verify");
        sb.AppendLine("1. Review the cluster details and associated log events");
        sb.AppendLine("2. Implement the fix based on the cluster type and severity");
        sb.AppendLine("3. Re-run log analysis to confirm cluster is resolved");
        sb.AppendLine("4. Monitor for 24 hours to ensure no recurrence");
        sb.AppendLine();

        sb.AppendLine($"**Generated**: {DateTime.UtcNow:O}");

        return sb.ToString();
    }
}
