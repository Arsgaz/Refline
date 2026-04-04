using System.Text;
using Refline.Data.Activity;
using Refline.Data.Infrastructure;
using Refline.Data.Reports;
using Refline.Data.Settings;
using System.IO;

namespace Refline.Business.Reports;

public class ReportBusinessServer : IReportBusinessServer
{
    private readonly IActivityDataService _activityDataService;
    private readonly ISettingsDataService _settingsDataService;
    private readonly IReportDataService _reportDataService;
    private readonly ReportValidationService _validationService;

    public ReportBusinessServer(
        IActivityDataService activityDataService,
        ISettingsDataService settingsDataService,
        IReportDataService reportDataService,
        ReportValidationService validationService)
    {
        _activityDataService = activityDataService;
        _settingsDataService = settingsDataService;
        _reportDataService = reportDataService;
        _validationService = validationService;
    }

    public OperationResult<string> ExportTodayReport()
    {
        var settingsResult = _settingsDataService.Load();
        if (!settingsResult.IsSuccess || settingsResult.Value == null)
        {
            return OperationResult<string>.Failure(settingsResult.Message, settingsResult.ErrorCode);
        }

        var validation = _validationService.ValidateExportPath(settingsResult.Value.ReportsPath);
        if (!validation.IsSuccess)
        {
            return OperationResult<string>.Failure(validation.Message, validation.ErrorCode);
        }

        try
        {
            Directory.CreateDirectory(settingsResult.Value.ReportsPath);
        }
        catch (Exception ex)
        {
            return OperationResult<string>.Failure($"Не удалось подготовить папку отчётов: {ex.Message}", "REPORT_DIR_ERROR");
        }

        var activityResult = _activityDataService.LoadByDate(DateTime.Today);
        if (!activityResult.IsSuccess || activityResult.Value == null)
        {
            return OperationResult<string>.Failure(activityResult.Message, activityResult.ErrorCode);
        }

        var activities = activityResult.Value.OrderByDescending(a => a.TimeSpentSeconds).ToList();
        var totalSeconds = activities.Sum(a => a.TimeSpentSeconds);
        var totalTs = TimeSpan.FromSeconds(totalSeconds);

        var sb = new StringBuilder();
        sb.AppendLine("--- Отчёт активности Refline ---");
        sb.AppendLine($"Дата: {DateTime.Now:dd.MM.yyyy HH:mm:ss}");
        sb.AppendLine($"Отработано сегодня: {(int)totalTs.TotalHours} ч {totalTs.Minutes:D2} мин");
        sb.AppendLine();
        sb.AppendLine("Активные приложения:");

        var index = 1;
        foreach (var activity in activities)
        {
            sb.AppendLine($"{index++}. {activity.AppName} - {activity.DurationString}");
        }

        var fileName = $"Refline_Report_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt";
        var fullPath = Path.Combine(settingsResult.Value.ReportsPath, fileName);

        var saveResult = _reportDataService.SaveTextReport(fullPath, sb.ToString());
        if (!saveResult.IsSuccess)
        {
            return OperationResult<string>.Failure(saveResult.Message, saveResult.ErrorCode);
        }

        return OperationResult<string>.Success(fullPath, "Отчёт успешно сохранён.");
    }
}
