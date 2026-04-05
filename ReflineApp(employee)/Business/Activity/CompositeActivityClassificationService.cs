using Refline.Models;

namespace Refline.Business.Activity;

public sealed class CompositeActivityClassificationService : IActivityClassificationService
{
    private readonly ICompanyActivityClassificationService _companyClassificationService;
    private readonly ActivityClassificationService _builtInClassificationService;

    public CompositeActivityClassificationService(
        ICompanyActivityClassificationService companyClassificationService,
        ActivityClassificationService builtInClassificationService)
    {
        _companyClassificationService = companyClassificationService;
        _builtInClassificationService = builtInClassificationService;
    }

    public ActivityCategory Classify(string appName, string? windowTitle)
    {
        return ClassifyDetailed(appName, windowTitle).Category;
    }

    public ActivityClassificationDecision ClassifyDetailed(string appName, string? windowTitle)
    {
        var companyDecision = _companyClassificationService.TryClassifyDetailed(appName, windowTitle);
        if (companyDecision != null)
        {
            return companyDecision;
        }

        var builtInDecision = _builtInClassificationService.ClassifyDetailed(appName, windowTitle);
        return builtInDecision.Category != ActivityCategory.Unknown
            ? builtInDecision
            : new ActivityClassificationDecision
            {
                Category = ActivityCategory.Unknown,
                Source = ActivityClassificationSource.FallbackUnknown
            };
    }

    public string NormalizeApplicationName(string windowTitle, bool isIdle)
    {
        return _builtInClassificationService.NormalizeApplicationName(windowTitle, isIdle);
    }
}
