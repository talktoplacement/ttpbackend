using CareerPlatform.Api.Features.Settings.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CareerPlatform.Api.Infrastructure.Persistence.Seed;

/// <summary>
/// Seeds the default catalog of admin platform settings (general / notifications / payment /
/// security) so every settings page has DB-backed rows to render on first boot. Existence is
/// checked per <c>Key</c>, so repeated runs never duplicate a row and — critically — never
/// overwrite a value an admin has since edited (Req 17.8). Adding a new setting later is a
/// data-only change: append a definition here (or insert a row via SQL) and it appears on the
/// matching category page automatically.
/// </summary>
public sealed class PlatformSettingSeeder : ISeeder
{
    private readonly AppDbContext _db;
    private readonly ILogger<PlatformSettingSeeder> _logger;

    public PlatformSettingSeeder(AppDbContext db, ILogger<PlatformSettingSeeder> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>Runs after roles/admin/plans.</summary>
    public int Order => 60;

    private sealed record Definition(
        string Key, string Category, string Label, string Description,
        SettingValueType ValueType, string Value, int DisplayOrder);

    private static readonly Definition[] Defaults =
    {
        // --- general ---
        new("general.platformName", "general", "Platform Name",
            "Displayed in the browser title and branding.", SettingValueType.String, "PlacementPro", 10),
        new("general.supportEmail", "general", "Public Support Email",
            "Address shown to users for support enquiries.", SettingValueType.String, "", 20),
        new("general.baseUrl", "general", "Platform Base URL",
            "Canonical public URL of the platform.", SettingValueType.String, "", 30),

        // --- notifications ---
        new("notifications.emailEnabled", "notifications", "Transactional Emails",
            "Send automated receipts, verification, and reminder emails.", SettingValueType.Boolean, "true", 10),
        new("notifications.smsEnabled", "notifications", "SMS / WhatsApp Alerts",
            "Send pre-meeting reminder notifications to students.", SettingValueType.Boolean, "false", 20),

        // --- payment ---
        new("payment.razorpayKeyId", "payment", "Razorpay Key ID",
            "Publishable Razorpay key id (safe to expose; never the secret key).",
            SettingValueType.String, "", 10),
        new("payment.gstRatePercent", "payment", "Default GST Rate (%)",
            "Tax percentage applied to invoices.", SettingValueType.Number, "18", 20),
        new("payment.autoInvoice", "payment", "Automated Invoice Dispatch",
            "Email a PDF tax invoice on successful settlement.", SettingValueType.Boolean, "true", 30),

        // --- security ---
        new("security.enforceTwoFactor", "security", "Enforce 2FA for Admins",
            "Require an authenticator code on admin login.", SettingValueType.Boolean, "false", 10),
        new("security.strictProctoring", "security", "Strict Test Proctoring",
            "Terminate a test session after repeated focus loss.", SettingValueType.Boolean, "true", 20),
    };

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        var existingKeys = await _db.PlatformSettings
            .Select(s => s.Key)
            .ToListAsync(cancellationToken);
        var existing = new HashSet<string>(existingKeys, StringComparer.Ordinal);

        var added = 0;
        foreach (var d in Defaults)
        {
            if (existing.Contains(d.Key))
            {
                continue;
            }

            _db.PlatformSettings.Add(new PlatformSetting
            {
                Key = d.Key,
                Category = d.Category,
                Label = d.Label,
                Description = d.Description,
                ValueType = d.ValueType,
                Value = d.Value,
                DisplayOrder = d.DisplayOrder,
            });
            added++;
        }

        if (added > 0)
        {
            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("PlatformSettingSeeder: seeded {Count} new setting(s).", added);
        }
        else
        {
            _logger.LogInformation("PlatformSettingSeeder: all settings already present; nothing to do.");
        }
    }
}
