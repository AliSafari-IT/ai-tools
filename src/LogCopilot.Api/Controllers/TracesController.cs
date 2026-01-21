using LogCopilot.Application.DTOs;
using LogCopilot.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogCopilot.Api.Controllers;

[ApiController]
[Route("api/traces")]
[Authorize]
public class TracesController : ControllerBase
{
    private readonly ITraceService _traceService;
    private readonly ILogger<TracesController> _logger;

    public TracesController(ITraceService traceService, ILogger<TracesController> logger)
    {
        _traceService = traceService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetTraces([FromQuery] TraceQueryDto query)
    {
        try
        {
            var result = await _traceService.GetTracesAsync(query);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting traces");
            return StatusCode(500, new { message = "Error retrieving traces", error = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetTraceById(Guid id)
    {
        try
        {
            var result = await _traceService.GetTraceByIdAsync(id);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting trace {TraceId}", id);
            return StatusCode(500, new { message = "Error retrieving trace", error = ex.Message });
        }
    }
}
