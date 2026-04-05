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
        return ExportReport(DateTime.Today, DateTime.Today, "Сегодня");
    }

    public OperationResult<string> ExportReport(DateTime startDate, DateTime endDate, string periodLabel)
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

        var normalizedStartDate = startDate.Date;
        var normalizedEndDate = endDate.Date;
        if (normalizedStartDate > normalizedEndDate)
        {
            (normalizedStartDate, normalizedEndDate) = (normalizedEndDate, normalizedStartDate);
        }

        var activityResult = _activityDataService.LoadByDateRange(normalizedStartDate, normalizedEndDate);
        if (!activityResult.IsSuccess || activityResult.Value == null)
        {
            return OperationResult<string>.Failure(activityResult.Message, activityResult.ErrorCode);
        }

        var activities = activityResult.Value
            .GroupBy(a => string.IsNullOrWhiteSpace(a.AppName) ? "Неизвестное приложение" : a.AppName.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(group => new
            {
                AppName = group.First().AppName,
                TotalSeconds = group.Sum(a => a.TimeSpentSeconds)
            })
            .OrderByDescending(a => a.TotalSeconds)
            .ThenBy(a => a.AppName, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var totalSeconds = activities.Sum(a => a.TotalSeconds);
        var totalTs = TimeSpan.FromSeconds(totalSeconds);

        var sb = new StringBuilder();
        sb.AppendLine("--- Отчёт активности Refline ---");
        sb.AppendLine($"Дата формирования: {DateTime.Now:dd.MM.yyyy HH:mm:ss}");
        sb.AppendLine($"Период: {periodLabel}");
        sb.AppendLine($"Диапазон: {normalizedStartDate:dd.MM.yyyy} - {normalizedEndDate:dd.MM.yyyy}");
        sb.AppendLine($"Отработано за период: {(int)totalTs.TotalHours} ч {totalTs.Minutes:D2} мин");
        sb.AppendLine();
        sb.AppendLine("Активные приложения:");

        var index = 1;
        foreach (var activity in activities)
        {
            var duration = TimeSpan.FromSeconds(activity.TotalSeconds);
            sb.AppendLine($"{index++}. {activity.AppName} - {(int)duration.TotalHours} ч {duration.Minutes:D2} мин");
        }

        var fileName = $"Refline_Report_{normalizedStartDate:yyyy-MM-dd}_{normalizedEndDate:yyyy-MM-dd}_{DateTime.Now:HH-mm-ss}.txt";
        var fullPath = Path.Combine(settingsResult.Value.ReportsPath, fileName);

        var saveResult = _reportDataService.SaveTextReport(fullPath, sb.ToString());
        if (!saveResult.IsSuccess)
        {
            return OperationResult<string>.Failure(saveResult.Message, saveResult.ErrorCode);
        }

        return OperationResult<string>.Success(fullPath, "Отчёт успешно сохранён.");
    }
}
