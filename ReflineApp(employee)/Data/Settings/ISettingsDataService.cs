using Refline.Data.Infrastructure;
using Refline.Models;

namespace Refline.Data.Settings;

public interface ISettingsDataService
{
    OperationResult<AppSettings> Load();
    OperationResult Save(AppSettings settings);
}
