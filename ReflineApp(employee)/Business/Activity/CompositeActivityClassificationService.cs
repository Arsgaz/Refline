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
        var companyCategory = _companyClassificationService.TryClassify(appName, windowTitle);
        if (companyCategory.HasValue)
        {
            return companyCategory.Value;
        }

        var builtInCategory = _builtInClassificationService.Classify(appName, windowTitle);
        return builtInCategory != ActivityCategory.Unknown
            ? builtInCategory
            : ActivityCategory.Unknown;
    }

    public string NormalizeApplicationName(string windowTitle, bool isIdle)
    {
        return _builtInClassificationService.NormalizeApplicationName(windowTitle, isIdle);
    }
}
