using System.Security.Claims;

namespace LogCopilot.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var claim = user.FindFirst(ClaimTypes.NameIdentifier);
        return claim != null ? Guid.Parse(claim.Value) : Guid.Empty;
    }

    public static Guid GetOrganizationId(this ClaimsPrincipal user)
    {
        var claim = user.FindFirst("OrganizationId") ?? user.FindFirst("organizationId");
        return claim != null ? Guid.Parse(claim.Value) : Guid.Empty;
    }

    public static bool IsAdmin(this ClaimsPrincipal user)
    {
        return user.IsInRole("Admin");
    }

    public static string GetRole(this ClaimsPrincipal user)
    {
        var claim = user.FindFirst(ClaimTypes.Role) ?? user.FindFirst("role");
        return claim?.Value ?? "Member";
    }
}
