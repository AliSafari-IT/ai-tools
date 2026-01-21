namespace LogCopilot.Infrastructure.Licensing;

public interface ILicenseVerifier
{
    Task<LicenseInfo> VerifyAsync();
}

public class LicenseInfo
{
    public string Edition { get; set; } = "Community";
    public bool IsValid { get; set; } = true;
    public string? ErrorMessage { get; set; }
}
