using LogCopilot.Api.Extensions;
using LogCopilot.Domain.Entities;
using LogCopilot.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LogCopilot.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly LogCopilotDbContext _context;
    private readonly ILogger<AdminController> _logger;

    public AdminController(LogCopilotDbContext context, ILogger<AdminController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet("members")]
    public async Task<IActionResult> GetMembers()
    {
        try
        {
            var organizationId = User.GetOrganizationId();
            var members = await _context.OrganizationMembers
                .Include(m => m.User)
                .Where(m => m.OrganizationId == organizationId)
                .Select(m => new MemberDto
                {
                    Id = m.Id,
                    UserId = m.UserId,
                    Email = m.User.Email,
                    FirstName = m.User.FirstName,
                    LastName = m.User.LastName,
                    Role = m.Role.ToString(),
                    CreatedAt = m.CreatedAt
                })
                .ToListAsync();

            return Ok(members);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching members");
            return StatusCode(500, new { message = "Error fetching members", error = ex.Message });
        }
    }

    [HttpPut("members/{id}/role")]
    public async Task<IActionResult> UpdateMemberRole(Guid id, [FromBody] UpdateRoleRequest request)
    {
        try
        {
            var organizationId = User.GetOrganizationId();
            var userId = User.GetUserId();

            var member = await _context.OrganizationMembers
                .FirstOrDefaultAsync(m => m.Id == id && m.OrganizationId == organizationId);

            if (member == null)
                return NotFound(new { message = "Member not found" });

            if (member.UserId == userId)
            {
                var adminCount = await _context.OrganizationMembers
                    .CountAsync(m => m.OrganizationId == organizationId && m.Role == Role.Admin);
                
                if (adminCount == 1 && request.Role != "Admin")
                    return BadRequest(new { message = "Cannot remove the last admin from the organization" });
            }

            if (!Enum.TryParse<Role>(request.Role, out var newRole))
                return BadRequest(new { message = "Invalid role. Must be 'Admin' or 'Member'" });

            member.Role = newRole;
            member.UpdatedAt = DateTime.UtcNow;
            member.UpdatedBy = userId;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Role updated successfully", role = newRole.ToString() });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating member role");
            return StatusCode(500, new { message = "Error updating member role", error = ex.Message });
        }
    }
}

public class MemberDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class UpdateRoleRequest
{
    public string Role { get; set; } = string.Empty;
}
