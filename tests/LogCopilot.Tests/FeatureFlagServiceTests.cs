using LogCopilot.Infrastructure.Features;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace LogCopilot.Tests;

public class FeatureFlagServiceTests
{
    [Fact]
    public void IsEnabled_ReturnsTrueWhenFeatureFlagIsTrue()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                { "Features:ProEnabled", "true" }
            })
            .Build();

        var service = new FeatureFlagService(config);
        Assert.True(service.IsEnabled("ProEnabled"));
    }

    [Fact]
    public void IsEnabled_ReturnsFalseWhenFeatureFlagIsFalse()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                { "Features:ProEnabled", "false" }
            })
            .Build();

        var service = new FeatureFlagService(config);
        Assert.False(service.IsEnabled("ProEnabled"));
    }

    [Fact]
    public void IsEnabled_ReturnsFalseWhenFeatureFlagNotSet()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>())
            .Build();

        var service = new FeatureFlagService(config);
        Assert.False(service.IsEnabled("NonExistentFeature"));
    }
}
