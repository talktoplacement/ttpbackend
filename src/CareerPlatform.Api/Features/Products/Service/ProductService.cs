using CareerPlatform.Api.Features.Products.Domain;
using CareerPlatform.Api.Features.Products.Dto;
using CareerPlatform.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CareerPlatform.Api.Features.Products.Service;

internal sealed class ProductService : IProductService
{
    private readonly AppDbContext _db;
    public ProductService(AppDbContext db) => _db = db;

    public async Task<Result<IReadOnlyList<ProductResponse>>> ListAsync(bool activeOnly, CancellationToken ct)
    {
        var q = _db.Products.AsNoTracking();
        if (activeOnly) q = q.Where(p => p.IsActive);
        var rows = await q
            .OrderBy(p => p.Name)
            .Take(PaginationRequest.MaxPageSize).ToListAsync(ct);
        return Result.Success((IReadOnlyList<ProductResponse>)rows.Select(ProductResponse.From).ToList());
    }

    public async Task<Result<ProductResponse>> GetAsync(int id, CancellationToken ct)
    {
        var p = await _db.Products.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (p is null)
            return Result.Failure<ProductResponse>(Error.NotFound("Product.NotFound", $"Product {id} was not found."));
        return Result.Success(ProductResponse.From(p));
    }

    public async Task<Result<ProductResponse>> CreateAsync(CreateProductRequest r, CancellationToken ct)
    {
        var code = r.Code.Trim().ToUpperInvariant();
        if (await _db.Products.AnyAsync(p => p.Code == code, ct))
            return Result.Failure<ProductResponse>(Error.Validation(
                "Product.CodeExists", $"A product with code '{code}' already exists."));
        var p = new Product
        {
            Code = code,
            Name = r.Name.Trim(),
            Description = r.Description?.Trim(),
            ProductType = r.ProductType.Trim().ToLowerInvariant(),
            Price = r.Price,
            Currency = r.Currency.Trim().ToUpperInvariant(),
            IsActive = r.IsActive,
        };
        _db.Products.Add(p);
        await _db.SaveChangesAsync(ct);
        return Result.Success(ProductResponse.From(p));
    }

    public async Task<Result<ProductResponse>> UpdateAsync(int id, UpdateProductRequest r, CancellationToken ct)
    {
        var p = await _db.Products.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (p is null)
            return Result.Failure<ProductResponse>(Error.NotFound("Product.NotFound", $"Product {id} was not found."));
        p.Name = r.Name.Trim();
        p.Description = r.Description?.Trim();
        p.ProductType = r.ProductType.Trim().ToLowerInvariant();
        p.Price = r.Price;
        p.Currency = r.Currency.Trim().ToUpperInvariant();
        p.IsActive = r.IsActive;
        await _db.SaveChangesAsync(ct);
        return Result.Success(ProductResponse.From(p));
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct)
    {
        var p = await _db.Products.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (p is null)
            return Result.Failure(Error.NotFound("Product.NotFound", $"Product {id} was not found."));
        _db.Products.Remove(p);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
