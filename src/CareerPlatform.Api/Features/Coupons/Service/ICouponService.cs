using CareerPlatform.Api.Features.Coupons.Dto;

namespace CareerPlatform.Api.Features.Coupons.Service;

public interface ICouponService
{
    Task<Result<IReadOnlyList<CouponResponse>>> ListAsync(CancellationToken ct);
    Task<Result<CouponResponse>> GetAsync(int id, CancellationToken ct);
    Task<Result<CouponResponse>> CreateAsync(CreateCouponRequest request, CancellationToken ct);
    Task<Result<CouponResponse>> UpdateAsync(int id, UpdateCouponRequest request, CancellationToken ct);
    Task<Result> DeleteAsync(int id, CancellationToken ct);
}
