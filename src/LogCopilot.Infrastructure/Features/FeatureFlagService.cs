using Microsoft.Extensions.Configuration;

namespace LogCopilot.Infrastructure.Features;

public interface IFeatureFlagService
{
    bool IsEnabled(string featureName);
}

public class FeatureFlagService : IFeatureFlagService
{
    private readonly IConfiguration _configuration;

    public FeatureFlagService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public bool IsEnabled(string featureName)
    {
        var value = _configuration[$"Features:{featureName}"];
        return bool.TryParse(value, out var result) && result;
    }
}
