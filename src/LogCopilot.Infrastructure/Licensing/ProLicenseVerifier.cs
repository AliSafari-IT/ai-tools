using Microsoft.Extensions.Configuration;

namespace LogCopilot.Infrastructure.Licensing;

public class ProLicenseVerifier : ILicenseVerifier
{
    private readonly IConfiguration _configuration;

    public ProLicenseVerifier(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public Task<LicenseInfo> VerifyAsync()
    {
        var licenseKey = _configuration["LICENSE_KEY"] ?? Environment.GetEnvironmentVariable("LICENSE_KEY");

        if (string.IsNullOrEmpty(licenseKey))
        {
            return Task.FromResult(new LicenseInfo
            {
                Edition = "Community",
                IsValid = false,
                ErrorMessage = "No license key provided. Running in Community mode."
            });
        }

        if (!ValidateLicenseFormat(licenseKey))
        {
            return Task.FromResult(new LicenseInfo
            {
                Edition = "Community",
                IsValid = false,
                ErrorMessage = "Invalid license key format. Running in Community mode."
            });
        }

        return Task.FromResult(new LicenseInfo
        {
            Edition = "Pro",
            IsValid = true,
            ErrorMessage = null
        });
    }

    private bool ValidateLicenseFormat(string licenseKey)
    {
        return licenseKey.StartsWith("LC-PRO-") && licenseKey.Length >= 20;
    }
}
