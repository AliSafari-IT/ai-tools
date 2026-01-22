using LogCopilot.Api.Extensions;
using LogCopilot.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LogCopilot.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BillingController : ControllerBase
{
    private readonly IBillingService _billingService;

    public BillingController(IBillingService billingService)
    {
        _billingService = billingService;
    }

    [HttpGet("plan")]
    public async Task<IActionResult> GetPlan()
    {
        var organizationId = User.GetOrganizationId();
        var planInfo = await _billingService.GetPlanInfoAsync(organizationId);
        return Ok(planInfo);
    }
}
