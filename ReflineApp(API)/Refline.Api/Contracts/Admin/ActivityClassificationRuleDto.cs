using Refline.Api.Enums;

namespace Refline.Api.Contracts.Admin;

public sealed record ActivityClassificationRuleDto(
    long Id,
    long CompanyId,
    string AppNamePattern,
    string? WindowTitlePattern,
    ActivityCategory Category,
    int Priority,
    bool IsEnabled,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
