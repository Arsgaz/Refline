using Refline.Data.Infrastructure;

namespace Refline.Data.Reports;

public interface IReportDataService
{
    OperationResult SaveTextReport(string fullPath, string content);
}
