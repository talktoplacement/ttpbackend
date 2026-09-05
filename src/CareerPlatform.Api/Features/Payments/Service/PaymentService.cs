using CareerPlatform.Api.Configuration;
using CareerPlatform.Api.Features.AdminLedger.Domain;
using CareerPlatform.Api.Features.Orders.Domain;
using CareerPlatform.Api.Features.Payments.Domain;
using CareerPlatform.Api.Features.Payments.Dto;
using CareerPlatform.Api.Features.SubscriptionPlans.Domain;
using CareerPlatform.Api.Infrastructure;
using CareerPlatform.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CareerPlatform.Api.Features.Payments.Service;

/// <summary>
/// Payment-gateway workflow. Ports the two legacy MediatR handlers verbatim (CreateOrder + Verify).
/// Verify enforces: (1) authenticated caller, (2) verify-signature-before-persist, (3) idempotency
/// on gateway order id, (4) plan must exist and be active, (5) supersede prior active
/// subscriptions before atomically writing Transaction + Subscription + refreshing the entitlement
/// cache in a single SaveChanges.
/// </summary>
internal sealed class PaymentService : IPaymentService
{
    private readonly AppDbContext _db;
    private readonly IPaymentGateway _gateway;
    private readonly ICurrentUser _currentUser;
    private readonly RazorpayOptions _options;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        AppDbContext db,
        IPaymentGateway gateway,
        ICurrentUser currentUser,
        IOptions<RazorpayOptions> options,
        ILogger<PaymentService> logger)
    {
        _db = db;
        _gateway = gateway;
        _currentUser = currentUser;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<Result<CreateOrderResponse>> CreateOrderAsync(int planId, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Result.Failure<CreateOrderResponse>(Error.Unauthorized(
                "Payment.Unauthorized", "An authenticated user is required to create an order."));
        }

        var plan = await _db.SubscriptionPlans.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == planId, ct);
        if (plan is null)
        {
            return Result.Failure<CreateOrderResponse>(Error.NotFound(
                "Plan.NotFound", $"No subscription plan exists with id {planId}."));
        }
        if (!plan.IsActive)
        {
            return Result.Failure<CreateOrderResponse>(Error.Validation(
                "Plan.Inactive", "The requested subscription plan is not available for purchase."));
        }

        var receiptId = $"rcpt_{Guid.NewGuid().ToString("N")[..12]}";
        var orderResult = await _gateway.CreateOrderAsync(plan.Price, receiptId, ct);
        if (orderResult.IsFailure)
        {
            _logger.LogWarning(
                "Order creation failed for user {UserId}, plan {PlanId}: {Code}",
                userId, plan.Id, orderResult.Error.Code);
            return Result.Failure<CreateOrderResponse>(orderResult.Error);
        }

        var gatewayOrderId = orderResult.Value;

        // Persist the order→plan→amount binding BEFORE returning to the browser.
        //
        // This row is what makes the callback trustworthy. Razorpay's signature covers only
        // {order_id}|{payment_id}, so without a stored binding the server has no way to know which
        // plan (or what amount) an order was for and would have to trust the client — which is
        // exactly how a caller could pay for the cheapest tier and claim the most expensive one.
        // The amount is snapshotted here too, so a price change mid-checkout cannot alter what the
        // student is recorded as having paid.
        _db.Orders.Add(new Order
        {
            UserId = userId,
            ProductType = OrderProductType.Plan,
            ProductId = plan.Id,
            Amount = plan.Price,
            RazorpayOrderId = gatewayOrderId,
            Status = OrderStatus.Created,
            CreatedAt = DateTime.UtcNow,
        });
        await _db.SaveChangesAsync(ct);

        return Result.Success(new CreateOrderResponse(
            gatewayOrderId,
            plan.Price,
            plan.Currency,
            _options.KeyId ?? string.Empty,
            plan.Id,
            plan.Code,
            plan.Name));
    }

    public async Task<Result<IReadOnlyList<StudentOrderResponse>>> ListMyOrdersAsync(CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Result.Failure<IReadOnlyList<StudentOrderResponse>>(Error.Unauthorized(
                "Payment.Unauthorized", "An authenticated user is required to list orders."));
        }

        var rows = await _db.OrderInvoices.AsNoTracking()
            .Where(o => o.CustomerUserId == userId)
            .OrderByDescending(o => o.PurchasedAtUtc)
            .Take(PaginationRequest.MaxPageSize)
            .ToListAsync(ct);
        return Result.Success((IReadOnlyList<StudentOrderResponse>)rows.Select(StudentOrderResponse.From).ToList());
    }

    public async Task<Result<StudentOrderResponse>> GetMyOrderAsync(int id, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Result.Failure<StudentOrderResponse>(Error.Unauthorized(
                "Payment.Unauthorized", "An authenticated user is required to view an order."));
        }

        var order = await _db.OrderInvoices.AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == id && o.CustomerUserId == userId, ct);
        if (order is null)
        {
            return Result.Failure<StudentOrderResponse>(Error.NotFound(
                "OrderInvoice.NotFound", $"Order {id} was not found in your history."));
        }
        return Result.Success(StudentOrderResponse.From(order));
    }

    public async Task<Result<VerifyPaymentResponse>> VerifyAsync(VerifyPaymentRequest request, CancellationToken ct)
    {
        // 0) Authenticated caller required; nothing is persisted otherwise.
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Result.Failure<VerifyPaymentResponse>(Error.Unauthorized(
                "Payment.Unauthorized", "An authenticated user is required to verify a payment."));
        }

        // 1) VERIFY-BEFORE-PERSIST — pure, no DB touched yet.
        var verification = _gateway.VerifySignature(new PaymentCallback(
            request.RazorpayOrderId, request.RazorpayPaymentId, request.RazorpaySignature));
        if (!verification.IsValid)
        {
            _logger.LogWarning(
                "Payment verification failed for user {UserId}, order {OrderId}: {Reason}",
                userId, request.RazorpayOrderId, verification.FailureReason);
            return Result.Failure<VerifyPaymentResponse>(Error.Validation(
                "Payment.SignatureInvalid", "Payment signature verification failed."));
        }

        // 2) IDEMPOTENCY — a duplicate gateway order id returns the original success.
        var existing = await _db.Transactions
            .FirstOrDefaultAsync(t => t.GatewayOrderId == request.RazorpayOrderId, ct);
        if (existing is not null)
        {
            _logger.LogInformation(
                "Idempotent repeat callback for user {UserId}, order {OrderId}: no new records",
                userId, request.RazorpayOrderId);
            return Result.Success(new VerifyPaymentResponse(
                true, "Already processed.", existing.PlanName));
        }

        // 3) Resolve the order we created for this checkout, SCOPED TO THE CALLER.
        //    The plan and the amount come from here — never from the request body — so a valid
        //    signature for a cheap order cannot be replayed to claim an expensive plan. Scoping by
        //    UserId also prevents one user from redeeming another user's order id.
        var order = await _db.Orders
            .FirstOrDefaultAsync(o =>
                o.RazorpayOrderId == request.RazorpayOrderId && o.UserId == userId, ct);
        if (order is null)
        {
            _logger.LogWarning(
                "Rejected payment for user {UserId}: no order {OrderId} belongs to this user",
                userId, request.RazorpayOrderId);
            return Result.Failure<VerifyPaymentResponse>(Error.Validation(
                "Payment.OrderNotFound",
                "No pending order matches this payment for the current user."));
        }

        // 4) Load the plan the order was created for.
        var plan = await _db.SubscriptionPlans
            .FirstOrDefaultAsync(p => p.Id == order.ProductId, ct);
        if (plan is null)
        {
            _logger.LogError(
                "Order {OrderId} references missing plan {PlanId}; cannot provision",
                request.RazorpayOrderId, order.ProductId);
            return Result.Failure<VerifyPaymentResponse>(Error.Validation(
                "Payment.InvalidPlan", "The purchased subscription plan is no longer available."));
        }

        // A plan retired between checkout and callback is still honoured: the student paid for it.
        // Only a missing plan (above) is fatal.
        if (!plan.IsActive)
        {
            _logger.LogInformation(
                "Provisioning order {OrderId} on plan {Code}, which has since been deactivated.",
                request.RazorpayOrderId, plan.Code);
        }

        var now = DateTime.UtcNow;

        // Close out the order with the gateway payment id.
        order.Status = OrderStatus.Paid;
        order.RazorpayPaymentId = request.RazorpayPaymentId;

        // 4) Supersede any prior active subscription so at most one remains.
        var priors = await _db.Subscriptions
            .Where(s => s.StudentId == userId && s.Status == SubscriptionStatus.Active)
            .ToListAsync(ct);
        foreach (var prior in priors)
        {
            prior.Supersede();
        }

        // 5) Atomic Transaction + Subscription graph, amount/currency from the stored plan only.
        // The amount recorded is the one snapshotted on the order — i.e. what Razorpay actually
        // charged — not the plan's current price, which may have been re-priced since checkout.
        var tx = new Transaction
        {
            UserId = userId,
            Amount = order.Amount,
            Currency = plan.Currency,
            PlanName = plan.Name,
            Date = now,
            GatewayOrderId = request.RazorpayOrderId,
        };
        _db.Transactions.Add(tx);

        var sub = Subscription.Activate(userId, plan, tx.Id, now);
        sub.Transaction = tx;
        _db.Subscriptions.Add(sub);

        // 6) Refresh the student's entitlement cache to the purchased plan.
        var user = await _db.UserProfiles
            .FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is not null)
        {
            user.PlanName = plan.Name;
        }

        // 7) Project the completed purchase into the admin order ledger. This is a denormalised
        //    read-model row (see Features/AdminLedger) so the admin orders page never has to join
        //    Transactions + Subscriptions + UserProfiles at request time. Written inside the same
        //    SaveChanges as the Transaction, so the ledger can never disagree with the payment —
        //    either both land or neither does. The idempotency guard at step 2 already returned
        //    early for a repeat callback, so this cannot double-insert (the unique index on
        //    OrderId is the backstop).
        _db.OrderInvoices.Add(new OrderInvoice
        {
            OrderId = request.RazorpayOrderId,
            CustomerUserId = userId,
            CustomerEmail = user?.Email,
            ItemDescription = plan.Name,
            Amount = order.Amount,
            Currency = plan.Currency,
            Status = OrderInvoiceStatus.Completed,
            PurchasedAtUtc = now,
        });

        // 8) Single atomic commit — Order + Transaction + Subscription + entitlement + ledger row.
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            // Two callbacks for the same order can race past the step-2 idempotency read. The unique
            // index on Transactions.GatewayOrderId is the backstop that prevents a double grant; the
            // loser must report the same success the winner did, not a 500 to a student who just paid.
            var winner = await _db.Transactions.AsNoTracking()
                .FirstOrDefaultAsync(t => t.GatewayOrderId == request.RazorpayOrderId, ct);
            if (winner is not null)
            {
                _logger.LogInformation(ex,
                    "Concurrent verify for order {OrderId} lost the race; returning the committed result.",
                    request.RazorpayOrderId);
                return Result.Success(new VerifyPaymentResponse(
                    true, "Already processed.", winner.PlanName));
            }
            throw;
        }

        _logger.LogInformation(
            "Provisioned subscription {SubId} on plan {Plan} for user {UserId}",
            sub.Id, plan.Name, userId);

        return Result.Success(new VerifyPaymentResponse(
            true, $"Plan '{plan.Name}' activated.", plan.Name));
    }
}
