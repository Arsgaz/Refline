using Refline.Api.Enums;

namespace Refline.Api.Contracts.ClassificationRules;

public sealed record EmployeeClassificationRuleDto(
    long Id,
    string AppNamePattern,
    string? WindowTitlePattern,
    ActivityCategory Category,
    int Priority);
