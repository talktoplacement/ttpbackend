using CareerPlatform.Api.Features.Certificates.Dto;

namespace CareerPlatform.Api.Features.Certificates.Service;

public interface ICertificateService
{
    Task<Result<IReadOnlyList<CertificateResponse>>> ListMineAsync(CancellationToken ct);
    Task<Result<CertificateResponse>> VerifyAsync(string code, CancellationToken ct);
    Task<Result<CertificateResponse>> IssueAsync(IssueCertificateRequest request, CancellationToken ct);
    Task<Result<DownloadCertificatePayload>> DownloadAsync(int id, bool allowAdmin, CancellationToken ct);
}
