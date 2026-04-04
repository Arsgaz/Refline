using Refline.Data.Infrastructure;

namespace Refline.Business.Reports;

public interface IReportBusinessServer
{
    OperationResult<string> ExportTodayReport();
}
