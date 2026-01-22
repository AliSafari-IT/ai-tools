using LogCopilot.Infrastructure.Licensing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace LogCopilot.Api.Controllers;

[ApiController]
[Route("api/admin/ai")]
[Authorize(Roles = "Admin")]
public class AdminAiController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ILicenseVerifier _licenseVerifier;

    public AdminAiController(IConfiguration configuration, ILicenseVerifier licenseVerifier)
    {
        _configuration = configuration;
        _licenseVerifier = licenseVerifier;
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetAiStatus()
    {
        var environment = _configuration["ASPNETCORE_ENVIRONMENT"] ?? "Production";
        var provider = _configuration["AI:Provider"] ?? "Community";
        var model = _configuration["AI:Model"] ?? "N/A";
        var endpoint = _configuration["AI:Endpoint"] ?? "N/A";
        var apiKey = _configuration["AI:ApiKey"];
        var hasApiKey = !string.IsNullOrWhiteSpace(apiKey);

        var licenseKey = _configuration["LICENSE_KEY"] ?? _configuration["License:Key"];
        var licenseKeyPresent = !string.IsNullOrWhiteSpace(licenseKey);
        var licensePrefix =
            licenseKeyPresent && licenseKey!.Length >= 8
                ? licenseKey.Substring(0, 8) + "..."
                : "N/A";

        var licenseInfo = await _licenseVerifier.VerifyAsync();
        var licenseValid = licenseInfo.IsValid && licenseInfo.Edition == "Pro";
        var licenseReason = licenseValid ? "Valid" : "Invalid or missing";

        var proEnabled = licenseValid && hasApiKey;

        return Ok(
            new
            {
                Environment = environment,
                Provider = provider,
                Model = model,
                Endpoint = endpoint != "N/A" ? MaskUrl(endpoint) : endpoint,
                HasApiKey = hasApiKey,
                ProEnabled = proEnabled,
                LicenseKeyPresent = licenseKeyPresent,
                LicenseKeyPrefix = licensePrefix,
                LicenseValid = licenseValid,
                LicenseValidationReason = licenseReason,
            }
        );
    }

    private string MaskUrl(string url)
    {
        if (string.IsNullOrEmpty(url))
            return url;
        var uri = new Uri(url);
        return $"{uri.Scheme}://{uri.Host}/...";
    }
}
