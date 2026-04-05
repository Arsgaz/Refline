using Refline.Models;

namespace Refline.Business.Activity;

public interface IActivityClassificationService
{
    ActivityCategory Classify(string appName, string? windowTitle);
    ActivityClassificationDecision ClassifyDetailed(string appName, string? windowTitle);
    string NormalizeApplicationName(string windowTitle, bool isIdle);
}
