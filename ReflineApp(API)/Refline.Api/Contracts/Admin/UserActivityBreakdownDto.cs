namespace Refline.Api.Contracts.Admin;

public sealed record UserActivityBreakdownDto(
    long UserId,
    DateOnly From,
    DateOnly To,
    IReadOnlyCollection<UserActivityApplicationDto> Applications,
    IReadOnlyCollection<UserActivityCategoryDto> Categories,
    IReadOnlyCollection<UserActivityDayDto> Days);

public sealed record UserActivityApplicationDto(
    string ApplicationName,
    int TotalSeconds);

public sealed record UserActivityCategoryDto(
    string Category,
    int TotalSeconds);

public sealed record UserActivityDayDto(
    DateOnly Date,
    int TotalSeconds,
    int ProductiveSeconds,
    int IdleSeconds);
