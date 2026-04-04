using Refline.Data.Infrastructure;
using System.IO;

namespace Refline.Business.Reports;

public class ReportValidationService
{
    public OperationResult ValidateExportPath(string reportsPath)
    {
        if (string.IsNullOrWhiteSpace(reportsPath))
        {
            return OperationResult.Failure("Путь сохранения отчётов не указан.", "REPORT_PATH_REQUIRED");
        }

        try
        {
            _ = Path.GetFullPath(reportsPath);
            return OperationResult.Success();
        }
        catch (Exception)
        {
            return OperationResult.Failure("Путь сохранения отчётов некорректен.", "REPORT_PATH_INVALID");
        }
    }
}
