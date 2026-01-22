namespace LogCopilot.Application.Interfaces;

public interface IApiKeyService
{
    Task<(string rawKey, ApiKeyDto keyDto)> CreateKeyAsync(Guid organizationId, string name, string[] scopes, DateTime? expiresAt, Guid createdBy);
    Task<ApiKeyValidationResult> ValidateKeyAsync(string rawKey);
    Task<List<ApiKeyDto>> GetKeysAsync(Guid organizationId);
    Task<bool> DeactivateKeyAsync(Guid keyId, Guid organizationId);
    Task<bool> DeleteKeyAsync(Guid keyId, Guid organizationId);
    Task UpdateLastUsedAsync(Guid keyId);
}

public class ApiKeyDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string KeyPrefix { get; set; } = string.Empty;
    public string[] Scopes { get; set; } = Array.Empty<string>();
    public bool IsActive { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ApiKeyValidationResult
{
    public bool IsValid { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid KeyId { get; set; }
    public string[] Scopes { get; set; } = Array.Empty<string>();
    public string? ErrorMessage { get; set; }
}
