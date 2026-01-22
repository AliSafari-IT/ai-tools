using LogCopilot.Api.Extensions;
using LogCopilot.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogCopilot.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class ApiKeysController : ControllerBase
{
    private readonly IApiKeyService _apiKeyService;

    public ApiKeysController(IApiKeyService apiKeyService)
    {
        _apiKeyService = apiKeyService;
    }

    [HttpGet]
    public async Task<IActionResult> GetKeys()
    {
        var organizationId = User.GetOrganizationId();
        var keys = await _apiKeyService.GetKeysAsync(organizationId);
        return Ok(keys);
    }

    [HttpPost]
    public async Task<IActionResult> CreateKey([FromBody] CreateApiKeyRequest request)
    {
        var organizationId = User.GetOrganizationId();
        var userId = User.GetUserId();
        
        var (rawKey, keyDto) = await _apiKeyService.CreateKeyAsync(
            organizationId,
            request.Name,
            request.Scopes,
            request.ExpiresAt,
            userId
        );

        return Ok(new { rawKey, key = keyDto });
    }

    [HttpPost("{id}/deactivate")]
    public async Task<IActionResult> DeactivateKey(Guid id)
    {
        var organizationId = User.GetOrganizationId();
        var success = await _apiKeyService.DeactivateKeyAsync(id, organizationId);
        
        if (!success)
            return NotFound();
        
        return Ok();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteKey(Guid id)
    {
        var organizationId = User.GetOrganizationId();
        var success = await _apiKeyService.DeleteKeyAsync(id, organizationId);
        
        if (!success)
            return NotFound();
        
        return Ok();
    }
}

public class CreateApiKeyRequest
{
    public string Name { get; set; } = string.Empty;
    public string[] Scopes { get; set; } = Array.Empty<string>();
    public DateTime? ExpiresAt { get; set; }
}
