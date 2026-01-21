namespace LogCopilot.Application.Interfaces;

public interface ICurrentUserService
{
    Guid UserId { get; }
    Guid OrganizationId { get; }
    bool IsAdmin { get; }
}
