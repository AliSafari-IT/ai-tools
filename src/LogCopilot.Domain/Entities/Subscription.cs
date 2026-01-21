namespace LogCopilot.Domain.Entities;

public class Subscription
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid BillingPlanId { get; set; }
    public SubscriptionStatus Status { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid CreatedBy { get; set; }
    public Guid UpdatedBy { get; set; }

    public BillingPlan BillingPlan { get; set; } = null!;
}

public enum SubscriptionStatus
{
    Trial = 0,
    Active = 1,
    Canceled = 2,
    Expired = 3
}
