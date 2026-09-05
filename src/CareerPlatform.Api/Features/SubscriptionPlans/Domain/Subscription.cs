using CareerPlatform.Api.Common;
using CareerPlatform.Api.Features.Payments.Domain;
using CareerPlatform.Api.Features.SubscriptionPlans.Domain.Events;
using CareerPlatform.Api.Features.Users.Domain;

namespace CareerPlatform.Api.Features.SubscriptionPlans.Domain;

/// <summary>
/// A provisioned, time-boxed entitlement mapping a student to a purchased plan (Req 9). Each
/// subscription snapshots the price and currency paid so later admin price edits never mutate this
/// historical record (Req 3.6, 8.4).
/// </summary>
public sealed class Subscription : AuditableEntity<int>
{
    /// <summary>FK → <see cref="UserProfile"/>.Id (Supabase user id) (Req 9.1, 13.2).</summary>
    public string StudentId { get; set; } = string.Empty;

    /// <summary>FK → <see cref="SubscriptionPlan"/>.Id (Req 9.1, 13.2).</summary>
    public int PlanId { get; set; }

    /// <summary>FK → <see cref="Transaction"/>.Id (Req 9.5, 13.2).</summary>
    public int TransactionId { get; set; }

    /// <summary>The UTC instant the subscription became active (Req 9.2).</summary>
    public DateTime StartDate { get; set; }

    /// <summary>Start plus the plan's billing period (Req 9.2).</summary>
    public DateTime EndDate { get; set; }

    /// <summary>Lifecycle status; new subscriptions start <see cref="SubscriptionStatus.Active"/> (Req 9.3).</summary>
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;

    /// <summary>Frozen snapshot of the price paid at purchase time (Req 3.6, 8.4).</summary>
    public decimal PricePaid { get; set; }

    /// <summary>Frozen snapshot of the currency at purchase time (Req 3.6, 8.4).</summary>
    public string Currency { get; set; } = "INR";

    public SubscriptionPlan? Plan { get; set; }
    public UserProfile? Student { get; set; }
    public Transaction? Transaction { get; set; }

    /// <summary>
    /// The single "is active now" rule: active status and the current instant within the period
    /// (Req 11.1).
    /// </summary>
    public bool IsActiveAt(DateTime utcNow) =>
        Status == SubscriptionStatus.Active && StartDate <= utcNow && utcNow < EndDate;

    /// <summary>
    /// Provisioning factory that builds an active subscription priced from the plan and raises the
    /// activation event (Req 9.1, 9.2, 9.3, 9.6).
    /// </summary>
    public static Subscription Activate(
        string studentId, SubscriptionPlan plan, int transactionId, DateTime startUtc)
    {
        ArgumentNullException.ThrowIfNull(plan);

        var sub = new Subscription
        {
            StudentId = studentId,
            PlanId = plan.Id,
            TransactionId = transactionId,
            StartDate = startUtc,
            EndDate = plan.ComputeEndDate(startUtc),
            Status = SubscriptionStatus.Active,
            PricePaid = plan.Price,
            Currency = plan.Currency,
        };

        sub.Raise(new SubscriptionActivatedDomainEvent(
            Guid.NewGuid(), DateTime.UtcNow, studentId, plan.Id, plan.Name));

        return sub;
    }

    /// <summary>Marks a prior active subscription as superseded by a newer purchase (Req 9.7).</summary>
    public void Supersede() => Status = SubscriptionStatus.Superseded;

    /// <summary>Marks the subscription as expired at end of its period (Req 11.2).</summary>
    public void Expire() => Status = SubscriptionStatus.Expired;
}
