using LogCopilot.Infrastructure.Licensing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace LogCopilot.Tests;

public class LicenseVerifierTests
{
    [Fact]
    public async Task CommunityLicenseVerifier_AlwaysReturnsCommunity()
    {
        var verifier = new CommunityLicenseVerifier();
        var result = await verifier.VerifyAsync();

        Assert.Equal("Community", result.Edition);
        Assert.True(result.IsValid);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public async Task ProLicenseVerifier_ReturnsCommunityWhenNoKeyProvided()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>())
            .Build();

        var verifier = new ProLicenseVerifier(config);
        var result = await verifier.VerifyAsync();

        Assert.Equal("Community", result.Edition);
        Assert.False(result.IsValid);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public async Task ProLicenseVerifier_ReturnsCommunityWhenKeyFormatInvalid()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                { "LICENSE_KEY", "invalid-key" }
            })
            .Build();

        var verifier = new ProLicenseVerifier(config);
        var result = await verifier.VerifyAsync();

        Assert.Equal("Community", result.Edition);
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task ProLicenseVerifier_ReturnsProWhenKeyFormatValid()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                { "LICENSE_KEY", "LC-PRO-1234567890abcdefghij" }
            })
            .Build();

        var verifier = new ProLicenseVerifier(config);
        var result = await verifier.VerifyAsync();

        Assert.Equal("Pro", result.Edition);
        Assert.True(result.IsValid);
        Assert.Null(result.ErrorMessage);
    }
}
