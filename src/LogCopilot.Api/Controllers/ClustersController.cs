using LogCopilot.Application.DTOs;
using LogCopilot.Application.Interfaces;
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
}
