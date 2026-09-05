using CareerPlatform.Api.Features.Support.Dto;

namespace CareerPlatform.Api.Features.Support.Service;

public interface ISupportService
{
    Task<Result<IReadOnlyList<SupportTicketResponse>>> ListMineAsync(CancellationToken ct);
    Task<Result<IReadOnlyList<SupportTicketResponse>>> ListAdminAsync(string? status, CancellationToken ct);
    Task<Result<SupportTicketResponse>> GetAsync(int id, bool allowAdmin, CancellationToken ct);
    Task<Result<SupportTicketResponse>> CreateAsync(CreateTicketRequest request, CancellationToken ct);
    Task<Result<SupportTicketResponse>> PostMessageAsync(int ticketId, string body, bool allowAdmin, CancellationToken ct);
    Task<Result<SupportTicketResponse>> UpdateStatusAsync(int id, UpdateTicketStatusRequest request, CancellationToken ct);
}
