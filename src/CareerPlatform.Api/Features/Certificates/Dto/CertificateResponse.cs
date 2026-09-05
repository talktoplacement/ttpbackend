using CareerPlatform.Api.Features.Certificates.Domain;

namespace CareerPlatform.Api.Features.Certificates.Dto;

public sealed record CertificateResponse(
    int Id,
    string Title,
    string? IssuedFor,
    string IssuedAt,
    string VerificationCode,
    bool HasDownloadableFile,
    string? RevokedAt)
{
    public static CertificateResponse From(Certificate c)
    {
        ArgumentNullException.ThrowIfNull(c);
        return new CertificateResponse(
            c.Id, c.Title, c.IssuedFor, c.IssuedAtUtc.ToString("O"),
            c.VerificationCode, !string.IsNullOrWhiteSpace(c.StorageKey),
            c.RevokedAtUtc?.ToString("O"));
    }
}

/// <summary>Body for <c>POST /api/v1/admin/certificates</c>.</summary>
public sealed record IssueCertificateRequest(string UserId, string Title, string? IssuedFor, string? StorageKey);

/// <summary>Return value for the download endpoints (rendered as a File response).</summary>
public sealed record DownloadCertificatePayload(byte[] Content, string FileName, string ContentType);
