namespace LogCopilot.Infrastructure.Licensing;

public class CommunityLicenseVerifier : ILicenseVerifier
{
    public Task<LicenseInfo> VerifyAsync()
    {
        return Task.FromResult(new LicenseInfo
        {
            Edition = "Community",
            IsValid = true,
            ErrorMessage = null
        });
    }
}
