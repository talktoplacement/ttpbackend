using CareerPlatform.Api.Features.Meetings.Dto;

namespace CareerPlatform.Api.Features.Meetings.Service;

public interface IMeetingService
{
    Task<Result<IReadOnlyList<MeetingResponse>>> ListAsync(CancellationToken ct);
    Task<Result<MeetingResponse>> ScheduleAsync(ScheduleMeetingRequest request, CancellationToken ct);
    Task<Result<MeetingResponse>> UpdateAsync(UpdateMeetingRequest request, CancellationToken ct);
    Task<Result> CancelAsync(int id, CancellationToken ct);
}
