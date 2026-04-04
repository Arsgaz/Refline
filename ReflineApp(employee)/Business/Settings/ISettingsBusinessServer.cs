using Refline.Data.Infrastructure;
using Refline.Models;

namespace Refline.Business.Settings;

public interface ISettingsBusinessServer
{
    OperationResult<AppSettings> LoadSettings();
    OperationResult SaveSettings(AppSettings settings);
    OperationResult<bool> IsBackgroundTrackingAllowed();
}
