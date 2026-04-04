using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Refline.Business.Activity;
using Refline.Business.Reports;
using Refline.Models;
using Refline.Services;
using Refline.Utils;
using SkiaSharp;

namespace Refline.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly IActivityBusinessServer _activityBusinessServer;
    private readonly IReportBusinessServer _reportBusinessServer;
    private readonly WindowTracker _windowTracker;
    private readonly Dispatcher _uiDispatcher;

    private bool _isTracking;
    private string _statusText = "Статус: остановлено";
    private string _startStopButtonContent = "▶ Старт";
    private ObservableCollection<AppActivity> _activities = new();

    private readonly DispatcherTimer _uiTimer;
    private TimeSpan _sessionTime;
    private string _sessionTimeString = "00:00:00";
    private string _todayTotalString = "0 ч 00 мин";
    private string _mostActiveAppName = "—";
    private string _totalTrackedTimeText = "00:00:00";
    private string _activeTimeText = "00:00:00";
    private string _idleTimeText = "00:00:00";
    private string _productiveTimeText = "00:00:00";
    private string _topApplicationText = "Нет данных";
    private string _topCategoryText = "Нет данных";
    private bool _hasCategoryChartData;
    private string _categoryChartPlaceholderText = "Категории пока не определены";
    private ISeries[] _categoryTimeSeries = Array.Empty<ISeries>();
    private ISeries[] _topApplicationsSeries = Array.Empty<ISeries>();
    private Axis[] _topApplicationsXAxes = Array.Empty<Axis>();
    private Axis[] _topApplicationsYAxes = Array.Empty<Axis>();

    public MainViewModel(
        IActivityBusinessServer activityBusinessServer,
        IReportBusinessServer reportBusinessServer,
        WindowTracker windowTracker)
    {
        _activityBusinessServer = activityBusinessServer;
        _reportBusinessServer = reportBusinessServer;
        _windowTracker = windowTracker;
        _uiDispatcher = Dispatcher.CurrentDispatcher;

        _windowTracker.OnWindowTracked += Tracker_OnWindowTracked;

        _uiTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _uiTimer.Tick += UiTimer_Tick;

        ToggleTrackingCommand = new RelayCommand(ExecuteToggleTracking);
        ExportCommand = new RelayCommand(ExecuteExport);

        LoadInitialData();
    }

    public ObservableCollection<AppActivity> Activities
    {
        get => _activities;
        set => SetProperty(ref _activities, value);
    }

    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    public string StartStopButtonContent
    {
        get => _startStopButtonContent;
        set => SetProperty(ref _startStopButtonContent, value);
    }

    public bool IsTracking
    {
        get => _isTracking;
        set => SetProperty(ref _isTracking, value);
    }

    public string SessionTimeString
    {
        get => _sessionTimeString;
        set => SetProperty(ref _sessionTimeString, value);
    }

    public string TodayTotalString
    {
        get => _todayTotalString;
        set => SetProperty(ref _todayTotalString, value);
    }

    public string MostActiveAppName
    {
        get => _mostActiveAppName;
        set => SetProperty(ref _mostActiveAppName, value);
    }

    public string TotalTrackedTimeText
    {
        get => _totalTrackedTimeText;
        set => SetProperty(ref _totalTrackedTimeText, value);
    }

    public string ActiveTimeText
    {
        get => _activeTimeText;
        set => SetProperty(ref _activeTimeText, value);
    }

    public string IdleTimeText
    {
        get => _idleTimeText;
        set => SetProperty(ref _idleTimeText, value);
    }

    public string ProductiveTimeText
    {
        get => _productiveTimeText;
        set => SetProperty(ref _productiveTimeText, value);
    }

    public string TopApplicationText
    {
        get => _topApplicationText;
        set => SetProperty(ref _topApplicationText, value);
    }

    public string TopCategoryText
    {
        get => _topCategoryText;
        set => SetProperty(ref _topCategoryText, value);
    }

    public bool HasCategoryChartData
    {
        get => _hasCategoryChartData;
        set => SetProperty(ref _hasCategoryChartData, value);
    }

    public string CategoryChartPlaceholderText
    {
        get => _categoryChartPlaceholderText;
        set => SetProperty(ref _categoryChartPlaceholderText, value);
    }

    public ISeries[] CategoryTimeSeries
    {
        get => _categoryTimeSeries;
        set => SetProperty(ref _categoryTimeSeries, value);
    }

    public ISeries[] TopApplicationsSeries
    {
        get => _topApplicationsSeries;
        set => SetProperty(ref _topApplicationsSeries, value);
    }

    public Axis[] TopApplicationsXAxes
    {
        get => _topApplicationsXAxes;
        set => SetProperty(ref _topApplicationsXAxes, value);
    }

    public Axis[] TopApplicationsYAxes
    {
        get => _topApplicationsYAxes;
        set => SetProperty(ref _topApplicationsYAxes, value);
    }

    public ICommand ToggleTrackingCommand { get; }
    public ICommand ExportCommand { get; }

    private void LoadInitialData()
    {
        RefreshReportData();
    }

    public void RefreshReportData()
    {
        var loadResult = _activityBusinessServer.LoadTodayActivities();
        if (loadResult.IsSuccess && loadResult.Value != null)
        {
            Activities = new ObservableCollection<AppActivity>(loadResult.Value.OrderByDescending(a => a.TimeSpentSeconds));
        }
        else
        {
            MessageBox.Show(loadResult.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        RefreshSummary();
    }

    private void ExecuteExport()
    {
        var exportResult = _reportBusinessServer.ExportTodayReport();
        if (exportResult.IsSuccess && !string.IsNullOrWhiteSpace(exportResult.Value))
        {
            MessageBox.Show(
                $"Отчёт успешно сохранен:\n{exportResult.Value}",
                "Успех",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        MessageBox.Show(exportResult.Message, "Ошибка экспорта", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    private void ExecuteToggleTracking()
    {
        if (IsTracking)
        {
            _windowTracker.Stop();
            _uiTimer.Stop();

            var result = MessageBox.Show(
                "Вы уверены, что хотите остановить трекинг и сбросить данные сессии?",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                var stopResult = _activityBusinessServer.StopTracking();
                if (!stopResult.IsSuccess)
                {
                    MessageBox.Show(stopResult.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                IsTracking = false;
                StartStopButtonContent = "▶ Старт";
                StatusText = "Статус: остановлено";
                _sessionTime = TimeSpan.Zero;
                SessionTimeString = _sessionTime.ToString(@"hh\:mm\:ss");
                AppLogger.Log("Tracking stopped.");
            }
            else
            {
                _windowTracker.Start();
                _uiTimer.Start();
            }

            return;
        }

        var startResult = _activityBusinessServer.StartTracking();
        if (!startResult.IsSuccess)
        {
            MessageBox.Show(startResult.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _sessionTime = TimeSpan.Zero;
        SessionTimeString = _sessionTime.ToString(@"hh\:mm\:ss");
        _uiTimer.Start();
        _windowTracker.Start();
        StartStopButtonContent = "⏸ Стоп";
        StatusText = "Статус: отслеживание идёт...";
        IsTracking = true;
        AppLogger.Log("Tracking started.");
    }

    private void UiTimer_Tick(object? sender, EventArgs e)
    {
        _sessionTime = _sessionTime.Add(TimeSpan.FromSeconds(1));
        SessionTimeString = _sessionTime.ToString(@"hh\:mm\:ss");
    }

    private void Tracker_OnWindowTracked(string windowTitle, bool isIdle)
    {
        _uiDispatcher.Invoke(() =>
        {
            var tickResult = _activityBusinessServer.ProcessWindowActivity(windowTitle, isIdle, DateTime.Now);
            if (!tickResult.IsSuccess || tickResult.Value == null)
            {
                StatusText = "Статус: ошибка обработки активности";
                return;
            }

            StatusText = tickResult.Value.StatusText;
            var updatedActivity = tickResult.Value.UpdatedActivity;

            if (updatedActivity != null)
            {
                UpsertActivity(updatedActivity, tickResult.Value.IsNewActivity);
            }

            ApplySummary(tickResult.Value.Summary);
        });
    }

    private void UpsertActivity(AppActivity updatedActivity, bool isNew)
    {
        if (isNew)
        {
            Activities.Add(updatedActivity);
        }
        else
        {
            var existing = Activities.FirstOrDefault(a => a.AppName == updatedActivity.AppName);
            if (existing != null)
            {
                existing.TimeSpentSeconds = updatedActivity.TimeSpentSeconds;
                existing.LastActive = updatedActivity.LastActive;
                existing.WindowTitle = updatedActivity.WindowTitle;
                existing.Category = updatedActivity.Category;
                existing.IsIdle = updatedActivity.IsIdle;
                existing.IsProductive = updatedActivity.IsProductive;
                existing.Version = updatedActivity.Version;
            }
            else
            {
                Activities.Add(updatedActivity);
            }
        }

        var sorted = Activities.OrderByDescending(a => a.TimeSpentSeconds).ToList();
        Activities = new ObservableCollection<AppActivity>(sorted);
    }

    private void RefreshSummary()
    {
        var summaryResult = _activityBusinessServer.GetTodaySummary();
        if (summaryResult.IsSuccess && summaryResult.Value != null)
        {
            ApplySummary(summaryResult.Value);
            return;
        }

        TodayTotalString = "0 ч 00 мин";
        MostActiveAppName = "—";
        TotalTrackedTimeText = "00:00:00";
        ActiveTimeText = "00:00:00";
        IdleTimeText = "00:00:00";
        ProductiveTimeText = "00:00:00";
        TopApplicationText = "Нет данных";
        TopCategoryText = "Нет данных";
        HasCategoryChartData = false;
        CategoryChartPlaceholderText = "Категории пока не определены";
        ApplyEmptyCharts();
    }

    private void ApplySummary(ActivitySummary summary)
    {
        TodayTotalString = summary.TodayTotalString;
        MostActiveAppName = summary.MostActiveAppName;
        TotalTrackedTimeText = FormatDuration(summary.Metrics.TotalTrackedSeconds);
        ActiveTimeText = FormatDuration(summary.Metrics.ActiveSeconds);
        IdleTimeText = FormatDuration(summary.Metrics.IdleSeconds);
        ProductiveTimeText = FormatDuration(summary.Metrics.ProductiveSeconds);
        TopApplicationText = string.IsNullOrWhiteSpace(summary.Metrics.TopApplicationName) ||
            summary.Metrics.TopApplicationName == "—"
                ? "Нет данных"
                : summary.Metrics.TopApplicationName;
        TopCategoryText = ToCategoryDisplayName(summary.Metrics.TopCategory);
        ApplyCharts(summary.Metrics);
    }

    private static string FormatDuration(int totalSeconds)
    {
        var safeSeconds = Math.Max(0, totalSeconds);
        var duration = TimeSpan.FromSeconds(safeSeconds);
        var totalHours = (int)duration.TotalHours;
        return $"{totalHours:D2}:{duration.Minutes:D2}:{duration.Seconds:D2}";
    }

    private static string ToCategoryDisplayName(ActivityCategory category)
    {
        return category switch
        {
            ActivityCategory.Work => "Работа",
            ActivityCategory.Communication => "Коммуникации",
            ActivityCategory.ConditionalWork => "Условная работа",
            ActivityCategory.Entertainment => "Развлечения",
            ActivityCategory.System => "Система",
            _ => "Нет данных"
        };
    }

    private void ApplyCharts(ActivityMetricsSummary metrics)
    {
        var pieSeries = metrics.CategorySeconds
            .Where(item => item.Value > 0)
            .OrderByDescending(item => item.Value)
            .Select(item => new PieSeries<int>
            {
                Name = ToCategoryDisplayName(item.Key),
                Values = new[] { item.Value },
                Fill = new SolidColorPaint(GetCategoryColor(item.Key)),
                Stroke = new SolidColorPaint(new SKColor(17, 24, 39)) { StrokeThickness = 2 },
                DataLabelsPaint = new SolidColorPaint(new SKColor(221, 230, 241)),
                DataLabelsSize = 13,
                ToolTipLabelFormatter = point => FormatDuration((int)point.Coordinate.PrimaryValue)
            })
            .Cast<ISeries>()
            .ToArray();

        HasCategoryChartData = pieSeries.Length > 0;
        CategoryChartPlaceholderText = "Категории пока не определены";

        var topApps = metrics.TopApplications
            .Where(item => item.TotalSeconds > 0)
            .Take(5)
            .ToList();

        var appLabels = topApps.Count == 0
            ? new[] { "Нет данных" }
            : topApps.Select(item => TrimChartLabel(item.ApplicationName)).ToArray();

        var appValues = topApps.Count == 0
            ? new[] { 0 }
            : topApps.Select(item => item.TotalSeconds).ToArray();

        CategoryTimeSeries = pieSeries;
        TopApplicationsSeries = new ISeries[]
        {
            new ColumnSeries<int>
            {
                Name = "Время",
                Values = appValues,
                Fill = new SolidColorPaint(new SKColor(45, 199, 255)),
                Stroke = null,
                MaxBarWidth = 42,
            }
        };
        TopApplicationsXAxes = new[]
        {
            new Axis
            {
                Labels = appLabels,
                LabelsPaint = new SolidColorPaint(new SKColor(155, 174, 194)),
                TextSize = 12,
                SeparatorsPaint = new SolidColorPaint(new SKColor(31, 41, 55))
            }
        };
        TopApplicationsYAxes = new[]
        {
            new Axis
            {
                MinLimit = 0,
                LabelsPaint = new SolidColorPaint(new SKColor(155, 174, 194)),
                TextSize = 12,
                Labeler = value => FormatDuration((int)value),
                SeparatorsPaint = new SolidColorPaint(new SKColor(31, 41, 55))
            }
        };
    }

    private void ApplyEmptyCharts()
    {
        HasCategoryChartData = false;
        CategoryChartPlaceholderText = "Категории пока не определены";
        CategoryTimeSeries = Array.Empty<ISeries>();
        TopApplicationsSeries = Array.Empty<ISeries>();
        TopApplicationsXAxes = new[]
        {
            new Axis
            {
                Labels = new[] { "Нет данных" },
                LabelsPaint = new SolidColorPaint(new SKColor(155, 174, 194)),
                TextSize = 12,
                SeparatorsPaint = new SolidColorPaint(new SKColor(31, 41, 55))
            }
        };
        TopApplicationsYAxes = new[]
        {
            new Axis
            {
                MinLimit = 0,
                LabelsPaint = new SolidColorPaint(new SKColor(155, 174, 194)),
                TextSize = 12,
                Labeler = value => FormatDuration((int)value),
                SeparatorsPaint = new SolidColorPaint(new SKColor(31, 41, 55))
            }
        };
    }

    private static string TrimChartLabel(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "Нет данных";
        }

        var trimmed = value.Trim();
        return trimmed.Length > 18 ? trimmed[..18] + "..." : trimmed;
    }

    private static SKColor GetCategoryColor(ActivityCategory category)
    {
        return category switch
        {
            ActivityCategory.Work => new SKColor(45, 199, 255),
            ActivityCategory.Communication => new SKColor(0, 255, 200),
            ActivityCategory.ConditionalWork => new SKColor(99, 102, 241),
            ActivityCategory.Entertainment => new SKColor(248, 113, 113),
            ActivityCategory.System => new SKColor(148, 163, 184),
            _ => new SKColor(55, 65, 81)
        };
    }

    public void OnClosing()
    {
        if (IsTracking)
        {
            _windowTracker.Stop();
        }

        var saveResult = _activityBusinessServer.SaveCurrentSession();
        if (!saveResult.IsSuccess)
        {
            AppLogger.Log(saveResult.Message, "ERROR");
        }
    }
}
