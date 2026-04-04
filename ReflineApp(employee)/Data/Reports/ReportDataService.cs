using Refline.Data.Infrastructure;
using System.IO;

namespace Refline.Data.Reports;

public class ReportDataService : IReportDataService
{
    public OperationResult SaveTextReport(string fullPath, string content)
    {
        try
        {
            File.WriteAllText(fullPath, content);
            return OperationResult.Success();
        }
        catch (Exception ex)
        {
            return OperationResult.Failure($"Ошибка сохранения отчёта: {ex.Message}", "REPORT_SAVE_ERROR");
        }
    }
}
