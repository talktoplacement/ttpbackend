using CareerPlatform.Api.Features.Products.Dto;

namespace CareerPlatform.Api.Features.Products.Service;

public interface IProductService
{
    Task<Result<IReadOnlyList<ProductResponse>>> ListAsync(bool activeOnly, CancellationToken ct);
    Task<Result<ProductResponse>> GetAsync(int id, CancellationToken ct);
    Task<Result<ProductResponse>> CreateAsync(CreateProductRequest request, CancellationToken ct);
    Task<Result<ProductResponse>> UpdateAsync(int id, UpdateProductRequest request, CancellationToken ct);
    Task<Result> DeleteAsync(int id, CancellationToken ct);
}
