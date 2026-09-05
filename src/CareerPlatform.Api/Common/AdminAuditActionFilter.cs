using System.Security.Claims;
using System.Text.Json;
using CareerPlatform.Api.Features.AdminLedger.Domain;
using CareerPlatform.Api.Features.AdminLedger.Service;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CareerPlatform.Api.Common;

/// <summary>
/// Global MVC filter that appends an <see cref="AdminAuditLog"/> row after every SUCCESSFUL
/// state-changing action on an admin-scoped controller.
///
/// Design decisions:
/// <list type="bullet">
/// <item>Runs AFTER the action (not before) so failed requests don't pollute the trail.</item>
/// <item>Only fires for POST / PUT / PATCH / DELETE — GETs are not privileged mutations.</item>
/// <item>Only fires when the route sits under <c>/api/v1/admin</c>. A student mutating their own
///       resume is not an audit event; an admin changing a coupon is.</item>
/// <item>Fail-soft: a logging failure never breaks the request. The whole point is observability,
///       and an audit-write outage must not take the API down with it.</item>
/// <item>Opt-out via <see cref="SkipAuditAttribute"/> on the controller or action.</item>
/// </list>
///
/// The action name is normalised into a stable verb (e.g. <c>COUPON_CREATED</c>) derived from the
/// controller + HTTP method, so the audit-log page can filter on a predictable vocabulary rather
/// than raw C# method names.
/// </summary>
public sealed class AdminAuditActionFilter : IAsyncActionFilter
{
    private const string AdminRoutePrefix = "api/v1/admin";
    private static readonly string[] AuditedMethods = { "POST", "PUT", "PATCH", "DELETE" };

    private readonly IServiceProvider _services;
    private readonly ILogger<AdminAuditActionFilter> _logger;

    public AdminAuditActionFilter(
        IServiceProvider services, ILogger<AdminAuditActionFilter> logger)
    {
        _services = services;
        _logger = logger;
    }

    public async Task OnActionExecutionAsync(
        ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var executed = await next();

        try
        {
            if (ShouldAudit(context, executed))
            {
                await WriteAuditRowAsync(context, executed);
            }
        }
        catch (Exception ex)
        {
            // Fail-soft by design — see class remarks.
            _logger.LogWarning(ex, "Audit-log append failed; request completed normally.");
        }
    }

    private static bool ShouldAudit(ActionExecutingContext context, ActionExecutedContext executed)
    {
        // Only successful responses.
        if (executed.Exception is not null) return false;
        var statusCode = executed.HttpContext.Response.StatusCode;
        if (statusCode is < 200 or >= 300) return false;

        // Only state-changing verbs.
        var method = context.HttpContext.Request.Method.ToUpperInvariant();
        if (!AuditedMethods.Contains(method)) return false;

        // Only admin-scoped routes.
        var path = context.HttpContext.Request.Path.Value ?? string.Empty;
        if (!path.TrimStart('/').StartsWith(AdminRoutePrefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // Respect explicit opt-out on the action or its controller.
        if (context.ActionDescriptor is ControllerActionDescriptor descriptor)
        {
            var hasSkip =
                descriptor.MethodInfo.GetCustomAttributes(typeof(SkipAuditAttribute), inherit: true).Length > 0
                || descriptor.ControllerTypeInfo.GetCustomAttributes(typeof(SkipAuditAttribute), inherit: true).Length > 0;
            if (hasSkip) return false;
        }

        return true;
    }

    private async Task WriteAuditRowAsync(
        ActionExecutingContext context, ActionExecutedContext executed)
    {
        // Resolved per-request from the action's own scope so the write shares the request's
        // DbContext lifetime.
        var ledger = context.HttpContext.RequestServices.GetService<IAdminLedgerService>();
        if (ledger is null) return;

        var user = context.HttpContext.User;
        var actorUserId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown";
        var actorEmail = user.FindFirstValue(ClaimTypes.Email);

        var (targetKind, action) = DeriveAction(context);
        var targetId = ExtractTargetId(context);

        var entry = new AdminAuditLog
        {
            ActorUserId = actorUserId,
            ActorEmail = actorEmail,
            Action = action,
            TargetKind = targetKind,
            TargetId = targetId,
            Metadata = SerialiseMetadata(context),
            IpAddress = context.HttpContext.Connection.RemoteIpAddress?.ToString(),
            OccurredAtUtc = DateTime.UtcNow,
        };

        await ledger.AppendAuditLogAsync(entry, context.HttpContext.RequestAborted);
    }

    /// <summary>
    /// Builds a stable action verb from the controller name + HTTP method, e.g.
    /// <c>AdminCouponsController</c> + POST → (<c>Coupon</c>, <c>COUPON_CREATED</c>).
    /// </summary>
    private static (string TargetKind, string Action) DeriveAction(ActionExecutingContext context)
    {
        var controllerName = context.RouteData.Values.TryGetValue("controller", out var c)
            ? c?.ToString() ?? "Unknown"
            : "Unknown";

        // "AdminCoupons" → "Coupon"; "AdminCms" → "Cms".
        var kind = controllerName;
        if (kind.StartsWith("Admin", StringComparison.OrdinalIgnoreCase) && kind.Length > 5)
        {
            kind = kind[5..];
        }
        if (kind.EndsWith('s') && kind.Length > 1)
        {
            kind = kind[..^1];
        }

        var verb = context.HttpContext.Request.Method.ToUpperInvariant() switch
        {
            "POST" => "CREATED",
            "PUT" => "UPDATED",
            "PATCH" => "UPDATED",
            "DELETE" => "DELETED",
            _ => "CHANGED",
        };

        return (kind, $"{kind.ToUpperInvariant()}_{verb}");
    }

    /// <summary>Pulls the resource id out of the route when the action has one.</summary>
    private static string? ExtractTargetId(ActionExecutingContext context)
    {
        foreach (var key in new[] { "id", "lessonId", "courseId", "questionId", "ticketId" })
        {
            if (context.RouteData.Values.TryGetValue(key, out var value) && value is not null)
            {
                return value.ToString();
            }
        }
        return null;
    }

    /// <summary>
    /// Serialises non-sensitive action arguments for forensic context. Any argument whose property
    /// name suggests a secret is redacted — an audit trail must never become a credential leak.
    /// </summary>
    private static string? SerialiseMetadata(ActionExecutingContext context)
    {
        if (context.ActionArguments.Count == 0) return null;

        var safe = new Dictionary<string, object?>();
        foreach (var (name, value) in context.ActionArguments)
        {
            if (value is null) continue;
            if (IsSensitiveName(name))
            {
                safe[name] = "[redacted]";
                continue;
            }
            safe[name] = Redact(value);
        }

        if (safe.Count == 0) return null;
        try
        {
            return JsonSerializer.Serialize(safe);
        }
        catch (NotSupportedException)
        {
            // Non-serialisable argument (e.g. IFormFile) — record its type instead of failing.
            return JsonSerializer.Serialize(
                safe.ToDictionary(kv => kv.Key, kv => kv.Value?.GetType().Name));
        }
    }

    private static object? Redact(object value)
    {
        var type = value.GetType();
        // Primitives and strings pass through untouched.
        if (type.IsPrimitive || value is string || value is decimal || value is DateTime)
        {
            return value;
        }

        var result = new Dictionary<string, object?>();
        foreach (var prop in type.GetProperties())
        {
            if (!prop.CanRead) continue;
            if (IsSensitiveName(prop.Name))
            {
                result[prop.Name] = "[redacted]";
                continue;
            }
            try
            {
                var propValue = prop.GetValue(value);
                result[prop.Name] = propValue is null || propValue is string || propValue.GetType().IsPrimitive
                    ? propValue
                    : propValue.ToString();
            }
            catch (Exception)
            {
                // A throwing getter must not break the audit write.
                result[prop.Name] = null;
            }
        }
        return result;
    }

    private static bool IsSensitiveName(string name) =>
        name.Contains("password", StringComparison.OrdinalIgnoreCase)
        || name.Contains("secret", StringComparison.OrdinalIgnoreCase)
        || name.Contains("token", StringComparison.OrdinalIgnoreCase)
        || name.Contains("apikey", StringComparison.OrdinalIgnoreCase)
        || name.Contains("signature", StringComparison.OrdinalIgnoreCase)
        || name.Contains("otp", StringComparison.OrdinalIgnoreCase);
}
