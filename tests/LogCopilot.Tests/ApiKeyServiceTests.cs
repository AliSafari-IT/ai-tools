using Xunit;
using LogCopilot.Infrastructure.Services;
using LogCopilot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LogCopilot.Tests;

public class ApiKeyServiceTests
{
    private LogCopilotDbContext GetInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<LogCopilotDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new LogCopilotDbContext(options);
    }

    [Fact]
    public async Task CreateKeyAsync_GeneratesValidKey()
    {
        var context = GetInMemoryContext();
        var service = new ApiKeyService(context);
        var orgId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var (rawKey, keyDto) = await service.CreateKeyAsync(
            orgId,
            "Test Key",
            new[] { "logs:read", "reports:read" },
            null,
            userId
        );

        Assert.NotEmpty(rawKey);
        Assert.StartsWith("lc_", rawKey);
        Assert.Equal("Test Key", keyDto.Name);
        Assert.True(keyDto.IsActive);
        Assert.Equal(2, keyDto.Scopes.Length);
    }

    [Fact]
    public async Task ValidateKeyAsync_ValidKey_ReturnsSuccess()
    {
        var context = GetInMemoryContext();
        var service = new ApiKeyService(context);
        var orgId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var (rawKey, _) = await service.CreateKeyAsync(
            orgId,
            "Test Key",
            new[] { "logs:read" },
            null,
            userId
        );

        var result = await service.ValidateKeyAsync(rawKey);

        Assert.True(result.IsValid);
        Assert.Equal(orgId, result.OrganizationId);
        Assert.Single(result.Scopes);
        Assert.Equal("logs:read", result.Scopes[0]);
    }

    [Fact]
    public async Task ValidateKeyAsync_InvalidKey_ReturnsFailure()
    {
        var context = GetInMemoryContext();
        var service = new ApiKeyService(context);

        var result = await service.ValidateKeyAsync("invalid_key_12345");

        Assert.False(result.IsValid);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public async Task ValidateKeyAsync_DeactivatedKey_ReturnsFailure()
    {
        var context = GetInMemoryContext();
        var service = new ApiKeyService(context);
        var orgId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var (rawKey, keyDto) = await service.CreateKeyAsync(
            orgId,
            "Test Key",
            new[] { "logs:read" },
            null,
            userId
        );

        await service.DeactivateKeyAsync(keyDto.Id, orgId);

        var result = await service.ValidateKeyAsync(rawKey);

        Assert.False(result.IsValid);
        Assert.Contains("inactive", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ValidateKeyAsync_ExpiredKey_ReturnsFailure()
    {
        var context = GetInMemoryContext();
        var service = new ApiKeyService(context);
        var orgId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var (rawKey, _) = await service.CreateKeyAsync(
            orgId,
            "Test Key",
            new[] { "logs:read" },
            DateTime.UtcNow.AddDays(-1),
            userId
        );

        var result = await service.ValidateKeyAsync(rawKey);

        Assert.False(result.IsValid);
        Assert.Contains("expired", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DeleteKeyAsync_RemovesKey()
    {
        var context = GetInMemoryContext();
        var service = new ApiKeyService(context);
        var orgId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var (rawKey, keyDto) = await service.CreateKeyAsync(
            orgId,
            "Test Key",
            new[] { "logs:read" },
            null,
            userId
        );

        var deleted = await service.DeleteKeyAsync(keyDto.Id, orgId);
        Assert.True(deleted);

        var result = await service.ValidateKeyAsync(rawKey);
        Assert.False(result.IsValid);
    }
}
