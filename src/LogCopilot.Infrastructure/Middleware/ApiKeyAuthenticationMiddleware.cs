using System.Security.Claims;
using LogCopilot.Application.Interfaces;
using Microsoft.AspNetCore.Http;

namespace LogCopilot.Infrastructure.Middleware;

public class ApiKeyAuthenticationMiddleware
{
    private readonly RequestDelegate _next;

    public ApiKeyAuthenticationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IApiKeyService apiKeyService)
    {
        if (context.Request.Headers.TryGetValue("X-Api-Key", out var apiKeyHeader))
        {
            var apiKey = apiKeyHeader.ToString();
            var validationResult = await apiKeyService.ValidateKeyAsync(apiKey);

            if (validationResult.IsValid)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, validationResult.KeyId.ToString()),
                    new Claim("OrganizationId", validationResult.OrganizationId.ToString()),
                    new Claim("organizationId", validationResult.OrganizationId.ToString()),
                    new Claim(ClaimTypes.Role, "ApiKey"),
                    new Claim("AuthType", "ApiKey")
                };

                foreach (var scope in validationResult.Scopes)
                {
                    claims.Add(new Claim("scope", scope));
                }

                var identity = new ClaimsIdentity(claims, "ApiKey");
                context.User = new ClaimsPrincipal(identity);

                await apiKeyService.UpdateLastUsedAsync(validationResult.KeyId);
            }
        }

        await _next(context);
    }
}
