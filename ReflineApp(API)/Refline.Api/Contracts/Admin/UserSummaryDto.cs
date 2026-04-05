namespace Refline.Api.Contracts.Admin;

public sealed record UserSummaryDto(
    long UserId,
    DateOnly From,
    DateOnly To,
    int TotalTrackedSeconds,
    int ProductiveSeconds,
    int IdleSeconds,
    int ActiveSeconds,
    string? TopApplicationName,
    string? TopCategory,
    int TotalRecordsCount);
