using System.Security.Cryptography;
using System.Text;
using LogCopilot.Application.Interfaces;
using LogCopilot.Domain.Entities;
using LogCopilot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LogCopilot.Infrastructure.Services;

public class ApiKeyService : IApiKeyService
{
    private readonly LogCopilotDbContext _context;

    public ApiKeyService(LogCopilotDbContext context)
    {
        _context = context;
    }

    public async Task<(string rawKey, ApiKeyDto keyDto)> CreateKeyAsync(
        Guid organizationId,
        string name,
        string[] scopes,
        DateTime? expiresAt,
        Guid createdBy
    )
    {
        var rawKey = GenerateApiKey();
        var keyHash = HashKey(rawKey);
        var keyPrefix = rawKey.Substring(0, 8);

        var apiKey = new ApiKey
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Name = name,
            KeyHash = keyHash,
            KeyPrefix = keyPrefix,
            Scopes = string.Join(",", scopes),
            IsActive = true,
            ExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = createdBy,
            UpdatedBy = createdBy,
        };

        _context.ApiKeys.Add(apiKey);
        await _context.SaveChangesAsync();

        var dto = new ApiKeyDto
        {
            Id = apiKey.Id,
            Name = apiKey.Name,
            KeyPrefix = apiKey.KeyPrefix,
            Scopes = scopes,
            IsActive = apiKey.IsActive,
            ExpiresAt = apiKey.ExpiresAt,
            LastUsedAt = apiKey.LastUsedAt,
            CreatedAt = apiKey.CreatedAt,
        };

        return (rawKey, dto);
    }

    public async Task<ApiKeyValidationResult> ValidateKeyAsync(string rawKey)
    {
        if (string.IsNullOrEmpty(rawKey) || rawKey.Length < 8)
        {
            return new ApiKeyValidationResult
            {
                IsValid = false,
                ErrorMessage = "Invalid key format",
            };
        }

        var keyPrefix = rawKey.Substring(0, 8);
        var keyHash = HashKey(rawKey);

        var apiKey = await _context.ApiKeys.FirstOrDefaultAsync(k =>
            k.KeyPrefix == keyPrefix && k.KeyHash == keyHash
        );

        if (apiKey == null)
        {
            return new ApiKeyValidationResult { IsValid = false, ErrorMessage = "Key not found" };
        }

        if (!apiKey.IsActive)
        {
            return new ApiKeyValidationResult { IsValid = false, ErrorMessage = "Key is inactive" };
        }

        if (apiKey.ExpiresAt.HasValue && apiKey.ExpiresAt.Value < DateTime.UtcNow)
        {
            return new ApiKeyValidationResult { IsValid = false, ErrorMessage = "Key has expired" };
        }

        var scopes = string.IsNullOrEmpty(apiKey.Scopes)
            ? Array.Empty<string>()
            : apiKey.Scopes.Split(',', StringSplitOptions.RemoveEmptyEntries);

        return new ApiKeyValidationResult
        {
            IsValid = true,
            OrganizationId = apiKey.OrganizationId,
            KeyId = apiKey.Id,
            Scopes = scopes,
        };
    }

    public async Task<List<ApiKeyDto>> GetKeysAsync(Guid organizationId)
    {
        var keys = await _context
            .ApiKeys.Where(k => k.OrganizationId == organizationId)
            .OrderByDescending(k => k.CreatedAt)
            .ToListAsync();

        return keys.Select(k => new ApiKeyDto
            {
                Id = k.Id,
                Name = k.Name,
                KeyPrefix = k.KeyPrefix,
                Scopes = string.IsNullOrEmpty(k.Scopes)
                    ? Array.Empty<string>()
                    : k.Scopes.Split(',', StringSplitOptions.RemoveEmptyEntries),
                IsActive = k.IsActive,
                ExpiresAt = k.ExpiresAt,
                LastUsedAt = k.LastUsedAt,
                CreatedAt = k.CreatedAt,
            })
            .ToList();
    }

    public async Task<bool> DeactivateKeyAsync(Guid keyId, Guid organizationId)
    {
        var key = await _context.ApiKeys.FirstOrDefaultAsync(k =>
            k.Id == keyId && k.OrganizationId == organizationId
        );

        if (key == null)
            return false;

        key.IsActive = false;
        key.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteKeyAsync(Guid keyId, Guid organizationId)
    {
        var key = await _context.ApiKeys.FirstOrDefaultAsync(k =>
            k.Id == keyId && k.OrganizationId == organizationId
        );

        if (key == null)
            return false;

        _context.ApiKeys.Remove(key);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task UpdateLastUsedAsync(Guid keyId)
    {
        var key = await _context.ApiKeys.FindAsync(keyId);
        if (key != null)
        {
            key.LastUsedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    private string GenerateApiKey()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return "lc_"
            + Convert.ToBase64String(bytes).Replace("+", "").Replace("/", "").Replace("=", "");
    }

    private string HashKey(string key)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(key);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
}
