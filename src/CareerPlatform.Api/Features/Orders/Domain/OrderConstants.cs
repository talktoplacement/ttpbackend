namespace CareerPlatform.Api.Features.Orders.Domain;

/// <summary>
/// The <c>Order.ProductType</c> vocabulary. Declared once so the value written at checkout and the
/// value filtered on later can never drift apart via a stray string literal.
/// </summary>
public static class OrderProductType
{
    /// <summary>A subscription plan purchase.</summary>
    public const string Plan = "Plan";

    /// <summary>A single-course purchase.</summary>
    public const string Course = "Course";
}

/// <summary>
/// The <c>Order.Status</c> lifecycle vocabulary.
/// </summary>
public static class OrderStatus
{
    /// <summary>Gateway order created; payment not yet verified.</summary>
    public const string Created = "Created";

    /// <summary>Payment signature verified and entitlement provisioned.</summary>
    public const string Paid = "Paid";

    /// <summary>Payment attempt abandoned or rejected by the gateway.</summary>
    public const string Failed = "Failed";
}
