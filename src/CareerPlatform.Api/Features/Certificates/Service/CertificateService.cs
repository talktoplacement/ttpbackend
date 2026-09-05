using System.Security.Cryptography;
using CareerPlatform.Api.Features.Certificates.Domain;
using CareerPlatform.Api.Features.Certificates.Dto;
using CareerPlatform.Api.Infrastructure;
using CareerPlatform.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CareerPlatform.Api.Features.Certificates.Service;

/// <summary>Certificates workflow. Ports the 4 legacy MediatR handlers verbatim.</summary>
internal sealed class CertificateService : ICertificateService
{
    private readonly AppDbContext _db;
    private readonly IFileStorage _storage;
    private readonly ICurrentUser _currentUser;

    public CertificateService(AppDbContext db, IFileStorage storage, ICurrentUser currentUser)
    {
        _db = db;
        _storage = storage;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<CertificateResponse>>> ListMineAsync(CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Result.Failure<IReadOnlyList<CertificateResponse>>(Error.Unauthorized(
                "Certificate.Unauthorized", "An authenticated user is required."));
        }

        var rows = await _db.Certificates.AsNoTracking()
            .Where(c => c.UserId == userId && c.RevokedAtUtc == null)
            .OrderByDescending(c => c.IssuedAtUtc)
            .Take(PaginationRequest.MaxPageSize)
            .ToListAsync(ct);
        IReadOnlyList<CertificateResponse> items = rows.Select(CertificateResponse.From).ToList();
        return Result.Success(items);
    }

    public async Task<Result<CertificateResponse>> VerifyAsync(string code, CancellationToken ct)
    {
        var c = code.Trim();
        var row = await _db.Certificates.AsNoTracking()
            .FirstOrDefaultAsync(cert => cert.VerificationCode == c && cert.RevokedAtUtc == null, ct);
        if (row is null)
        {
            return Result.Failure<CertificateResponse>(Error.NotFound(
                "Certificate.NotFound", "No active certificate matches that verification code."));
        }
        return Result.Success(CertificateResponse.From(row));
    }

    public async Task<Result<CertificateResponse>> IssueAsync(IssueCertificateRequest request, CancellationToken ct)
    {
        var userExists = await _db.UserProfiles.AsNoTracking()
            .AnyAsync(u => u.Id == request.UserId, ct);
        if (!userExists)
        {
            return Result.Failure<CertificateResponse>(Error.Validation(
                "Certificate.UserNotFound", $"User '{request.UserId}' does not exist."));
        }

        var cert = new Certificate
        {
            UserId = request.UserId,
            Title = request.Title.Trim(),
            IssuedFor = string.IsNullOrWhiteSpace(request.IssuedFor) ? null : request.IssuedFor.Trim(),
            StorageKey = string.IsNullOrWhiteSpace(request.StorageKey) ? null : request.StorageKey.Trim(),
            VerificationCode = GenerateVerificationCode(),
            IssuedAtUtc = DateTime.UtcNow,
        };
        _db.Certificates.Add(cert);
        await _db.SaveChangesAsync(ct);
        return Result.Success(CertificateResponse.From(cert));
    }

    public async Task<Result<DownloadCertificatePayload>> DownloadAsync(
        int id, bool allowAdmin, CancellationToken ct)
    {
        var row = await _db.Certificates.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, ct);
        if (row is null || row.RevokedAtUtc is not null)
        {
            return Result.Failure<DownloadCertificatePayload>(Error.NotFound(
                "Certificate.NotFound", $"Certificate {id} was not found."));
        }

        if (!allowAdmin)
        {
            var caller = _currentUser.UserId;
            if (string.IsNullOrEmpty(caller) ||
                !string.Equals(row.UserId, caller, StringComparison.Ordinal))
            {
                // Return 404 (not 403) so callers can't enumerate ids.
                return Result.Failure<DownloadCertificatePayload>(Error.NotFound(
                    "Certificate.NotFound", $"Certificate {id} was not found."));
            }
        }

        if (string.IsNullOrWhiteSpace(row.StorageKey))
        {
            return Result.Failure<DownloadCertificatePayload>(Error.NotFound(
                "Certificate.PdfNotAvailable",
                "This certificate has been issued but the PDF has not been uploaded yet."));
        }

        await using var source = await _storage.OpenAsync(row.StorageKey, ct);
        using var ms = new MemoryStream();
        await source.CopyToAsync(ms, ct);
        var fileName = $"certificate-{row.VerificationCode}.pdf";
        return Result.Success(new DownloadCertificatePayload(ms.ToArray(), fileName, "application/pdf"));
    }

    private static string GenerateVerificationCode()
    {
        const string alphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";
        Span<byte> bytes = stackalloc byte[12];
        RandomNumberGenerator.Fill(bytes);
        Span<char> chars = stackalloc char[12];
        for (var i = 0; i < 12; i++)
        {
            chars[i] = alphabet[bytes[i] % alphabet.Length];
        }
        return new string(chars);
    }
}
