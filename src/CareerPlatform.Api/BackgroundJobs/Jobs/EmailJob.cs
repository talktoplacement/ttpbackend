using CareerPlatform.Api.Infrastructure;
using Microsoft.Extensions.Logging;

namespace CareerPlatform.Api.BackgroundJobs.Jobs;

/// <summary>
/// Deferred bulk-email workload (Req 23.1). Carries a fully-materialised recipient list and one
/// rendered message body, and sends them off the request thread via <see cref="IEmailSender"/>
/// resolved from the per-job DI scope.
/// <para>
/// A per-recipient send failure is logged and skipped rather than aborting the batch: a single bad
/// address (or a transient provider rejection for one message) must not silently drop the
/// remaining recipients. The job is not durable across process restarts — see
/// <see cref="ChannelJobQueue"/>.
/// </para>
/// </summary>
public sealed class EmailJob : IJob
{
    private readonly IReadOnlyList<string> _recipients;
    private readonly string _subject;
    private readonly string _htmlBody;

    /// <param name="recipients">Non-empty e-mail addresses; duplicates and blanks are dropped.</param>
    /// <param name="subject">Rendered subject line.</param>
    /// <param name="htmlBody">Rendered HTML body, identical for every recipient.</param>
    public EmailJob(IEnumerable<string> recipients, string subject, string htmlBody)
    {
        ArgumentNullException.ThrowIfNull(recipients);
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);
        ArgumentException.ThrowIfNullOrWhiteSpace(htmlBody);

        _recipients = recipients
            .Where(address => !string.IsNullOrWhiteSpace(address))
            .Select(address => address.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        _subject = subject;
        _htmlBody = htmlBody;
    }

    public string JobType => "Email";

    public string JobId { get; } = Guid.NewGuid().ToString("N");

    public async Task ExecuteAsync(IServiceProvider services, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(services);

        var sender = (IEmailSender?)services.GetService(typeof(IEmailSender));
        var logger = ((ILoggerFactory?)services.GetService(typeof(ILoggerFactory)))
            ?.CreateLogger<EmailJob>();

        if (sender is null)
        {
            logger?.LogError(
                "EmailJob aborted: no IEmailSender is registered. JobId={JobId} Recipients={Count}",
                JobId, _recipients.Count);
            return;
        }

        var sent = 0;
        var failed = 0;

        foreach (var address in _recipients)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                await sender.SendAsync(new EmailMessage(address, _subject, _htmlBody), ct);
                sent++;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                failed++;
                // The address itself is not logged — it is user PII and the JobId is enough to
                // correlate the batch.
                logger?.LogWarning(ex,
                    "EmailJob delivery failed for one recipient. JobId={JobId}", JobId);
            }
        }

        logger?.LogInformation(
            "EmailJob completed. JobId={JobId} Sent={Sent} Failed={Failed}", JobId, sent, failed);
    }
}
