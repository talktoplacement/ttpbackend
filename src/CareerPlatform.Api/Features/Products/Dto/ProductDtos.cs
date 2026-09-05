using CareerPlatform.Api.Features.Products.Domain;

namespace CareerPlatform.Api.Features.Products.Dto;

public sealed record ProductResponse(
    int Id, string Code, string Name, string? Description,
    string ProductType, decimal Price, string Currency, bool IsActive)
{
    public static ProductResponse From(Product p)
    {
        ArgumentNullException.ThrowIfNull(p);
        return new ProductResponse(
            p.Id, p.Code, p.Name, p.Description,
            p.ProductType, p.Price, p.Currency, p.IsActive);
    }
}

public sealed record CreateProductRequest(
    string Code, string Name, string? Description,
    string ProductType, decimal Price,
    string Currency = "INR", bool IsActive = true);

public sealed record UpdateProductRequest(
    string Name, string? Description,
    string ProductType, decimal Price,
    string Currency, bool IsActive);
