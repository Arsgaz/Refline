using System.Collections.ObjectModel;
using System.Globalization;
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
using Refline.Services.ActivitySync;
using Refline.Utils;
using SkiaSharp;

namespace Refline.ViewModels;

public class MainViewModel : ViewModelBase
{
    private static readonly CultureInfo RussianCulture = new("ru-RU");

    private readonly IActivityBusinessServer _activityBusinessServer;
    private readonly IReportBusinessServer _reportBusinessServer;
    private readonly IActivitySyncService _activitySyncService;
    private readonly WindowTracker _windowTracker;
    private readonly Dispatcher _uiDispatcher;

    private bool _isTracking;
    private string _statusText = "Статус: остановлено";
    private string _startStopButtonContent = "▶ Старт";
    private ObservableCollection<AppActivity> _reportActivities = new();

    private readonly DispatcherTimer _uiTimer;
    private readonly DispatcherTimer _syncTimer;
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
    private string _currentTrackedAppText = "Нет данных";
    private string _currentTrackedWindowText = "Нет данных";
    private string _currentTrackedCategoryText = "Нет данных";
    private string _currentClassificationSourceText = "Нет данных";
    private string _currentMatchedRuleText = "—";

    private ReportPeriod _selectedPeriod = ReportPeriod.Day;
    private DateTime _selectedDate = DateTime.Today;
    private string _selectedPeriodLabel = string.Empty;
    private string _currentPeriodButtonText = "Сегодня";
    private string _reportTotalTrackedTimeText = "00:00:00";
    private string _reportActiveTimeText = "00:00:00";
    private string _reportIdleTimeText = "00:00:00";
    private string _reportProductiveTimeText = "00:00:00";
    private string _reportTopApplicationText = "Нет данных";
    private string _reportTopCategoryText = "Нет данных";
    private bool _hasCategoryChartData;
    private string _categoryChartPlaceholderText = "Категории пока не определены";
    private ISeries[] _categoryTimeSeries = Array.Empty<ISeries>();
    private ISeries[] _topApplicationsSeries = Array.Empty<ISeries>();
    private Axis[] _topApplicationsXAxes = Array.Empty<Axis>();
    private Axis[] _topApplicationsYAxes = Array.Empty<Axis>();
    private bool _showDailyTrendChart;
    private bool _hasDailyTrendChartData;
    private string _dailyTrendChartPlaceholderText = "График по дням доступен для недели и месяца";
    private ISeries[] _dailyTrendSeries = Array.Empty<ISeries>();
    private Axis[] _dailyTrendXAxes = Array.Empty<Axis>();
    private Axis[] _dailyTrendYAxes = Array.Empty<Axis>();

    private bool _isRefreshingReportData;
    private bool _hasPendingReportRefresh;

    public MainViewModel(
        IActivityBusinessServer activityBusinessServer,
        IReportBusinessServer reportBusinessServer,
        IActivitySyncService activitySyncService,
        WindowTracker windowTracker)
    {
        _activityBusinessServer = activityBusinessServer;
        _reportBusinessServer = reportBusinessServer;
        _activitySyncService = activitySyncService;
        _windowTracker = windowTracker;
        _uiDispatcher = Dispatcher.CurrentDispatcher;

        _windowTracker.OnWindowTracked += Tracker_OnWindowTracked;

        _uiTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _uiTimer.Tick += UiTimer_Tick;

        _syncTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(45)
        };
        _syncTimer.Tick += SyncTimer_Tick;
        _syncTimer.Start();

        ToggleTrackingCommand = new RelayCommand(ExecuteToggleTracking);
        ExportCommand = new RelayCommand(ExecuteExport);
        SetReportPeriodCommand = new RelayCommand(ExecuteSetReportPeriod);
        PreviousReportPeriodCommand = new RelayCommand(ExecutePreviousReportPeriod);
        NextReportPeriodCommand = new RelayCommand(ExecuteNextReportPeriod, CanExecuteNextReportPeriod);
        CurrentReportPeriodCommand = new RelayCommand(ExecuteCurrentReportPeriod, CanExecuteCurrentReportPeriod);

        UpdatePeriodPresentation();
        LoadInitialData();
        _ = TriggerSyncAsync("startup");
    }

    public ObservableCollection<AppActivity> ReportActivities
    {
        get => _reportActivities;
        set => SetProperty(ref _reportActivities, value);
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

    public string CurrentTrackedAppText
    {
        get => _currentTrackedAppText;
        set => SetProperty(ref _currentTrackedAppText, value);
    }

    public string CurrentTrackedWindowText
    {
        get => _currentTrackedWindowText;
        set => SetProperty(ref _currentTrackedWindowText, value);
    }

    public string CurrentTrackedCategoryText
    {
        get => _currentTrackedCategoryText;
        set => SetProperty(ref _currentTrackedCategoryText, value);
    }

    public string CurrentClassificationSourceText
    {
        get => _currentClassificationSourceText;
        set => SetProperty(ref _currentClassificationSourceText, value);
    }

    public string CurrentMatchedRuleText
    {
        get => _currentMatchedRuleText;
        set => SetProperty(ref _currentMatchedRuleText, value);
    }

    public ReportPeriod SelectedPeriod
    {
        get => _selectedPeriod;
        private set
        {
            if (SetProperty(ref _selectedPeriod, value))
            {
                OnPropertyChanged(nameof(IsDayPeriodSelected));
                OnPropertyChanged(nameof(IsWeekPeriodSelected));
                OnPropertyChanged(nameof(IsMonthPeriodSelected));
            }
        }
    }

    public DateTime SelectedDate
    {
        get => _selectedDate;
        private set => SetProperty(ref _selectedDate, value.Date);
    }

    public string SelectedPeriodLabel
    {
        get => _selectedPeriodLabel;
        private set => SetProperty(ref _selectedPeriodLabel, value);
    }

    public string CurrentPeriodButtonText
    {
        get => _currentPeriodButtonText;
        private set => SetProperty(ref _currentPeriodButtonText, value);
    }

    public bool IsDayPeriodSelected => SelectedPeriod == ReportPeriod.Day;
    public bool IsWeekPeriodSelected => SelectedPeriod == ReportPeriod.Week;
    public bool IsMonthPeriodSelected => SelectedPeriod == ReportPeriod.Month;

    public string ReportTotalTrackedTimeText
    {
        get => _reportTotalTrackedTimeText;
        private set => SetProperty(ref _reportTotalTrackedTimeText, value);
    }

    public string ReportActiveTimeText
    {
        get => _reportActiveTimeText;
        private set => SetProperty(ref _reportActiveTimeText, value);
    }

    public string ReportIdleTimeText
    {
        get => _reportIdleTimeText;
        private set => SetProperty(ref _reportIdleTimeText, value);
    }

    public string ReportProductiveTimeText
    {
        get => _reportProductiveTimeText;
        private set => SetProperty(ref _reportProductiveTimeText, value);
    }

    public string ReportTopApplicationText
    {
        get => _reportTopApplicationText;
        private set => SetProperty(ref _reportTopApplicationText, value);
    }

    public string ReportTopCategoryText
    {
        get => _reportTopCategoryText;
        private set => SetProperty(ref _reportTopCategoryText, value);
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

    public bool ShowDailyTrendChart
    {
        get => _showDailyTrendChart;
        private set => SetProperty(ref _showDailyTrendChart, value);
    }

    public bool HasDailyTrendChartData
    {
        get => _hasDailyTrendChartData;
        private set => SetProperty(ref _hasDailyTrendChartData, value);
    }

    public string DailyTrendChartPlaceholderText
    {
        get => _dailyTrendChartPlaceholderText;
        private set => SetProperty(ref _dailyTrendChartPlaceholderText, value);
    }

    public ISeries[] DailyTrendSeries
    {
        get => _dailyTrendSeries;
        private set => SetProperty(ref _dailyTrendSeries, value);
    }

    public Axis[] DailyTrendXAxes
    {
        get => _dailyTrendXAxes;
        private set => SetProperty(ref _dailyTrendXAxes, value);
    }

    public Axis[] DailyTrendYAxes
    {
        get => _dailyTrendYAxes;
        private set => SetProperty(ref _dailyTrendYAxes, value);
    }

    public ICommand ToggleTrackingCommand { get; }
    public ICommand ExportCommand { get; }
    public ICommand SetReportPeriodCommand { get; }
    public ICommand PreviousReportPeriodCommand { get; }
    public ICommand NextReportPeriodCommand { get; }
    public ICommand CurrentReportPeriodCommand { get; }

    private void LoadInitialData()
    {
        RefreshDashboardSummary();
        RefreshReportData();
    }

    public void RefreshReportData()
    {
        QueueReportRefresh();
    }

    private void ExecuteExport()
    {
        var range = GetSelectedRange();
        var exportResult = _reportBusinessServer.ExportReport(range.StartDate, range.EndDate, SelectedPeriodLabel);
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
                QueueReportRefresh();
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

    private void ExecuteSetReportPeriod(object? parameter)
    {
        if (!TryParseReportPeriod(parameter, out var period) || period == SelectedPeriod)
        {
            return;
        }

        SelectedPeriod = period;
        UpdatePeriodPresentation();
        QueueReportRefresh();
    }

    private void ExecutePreviousReportPeriod()
    {
        SelectedDate = ReportPeriodHelper.Move(SelectedPeriod, SelectedDate, -1);
        UpdatePeriodPresentation();
        QueueReportRefresh();
    }

    private void ExecuteNextReportPeriod()
    {
        if (!CanExecuteNextReportPeriod())
        {
            return;
        }

        SelectedDate = ReportPeriodHelper.Move(SelectedPeriod, SelectedDate, 1);
        UpdatePeriodPresentation();
        QueueReportRefresh();
    }

    private bool CanExecuteNextReportPeriod()
    {
        var currentRange = ReportPeriodHelper.GetRange(SelectedPeriod, DateTime.Today);
        var selectedRange = GetSelectedRange();
        return selectedRange.StartDate < currentRange.StartDate;
    }

    private void ExecuteCurrentReportPeriod()
    {
        SelectedDate = DateTime.Today;
        UpdatePeriodPresentation();
        QueueReportRefresh();
    }

    private bool CanExecuteCurrentReportPeriod()
    {
        var currentRange = ReportPeriodHelper.GetRange(SelectedPeriod, DateTime.Today);
        var selectedRange = GetSelectedRange();
        return selectedRange.StartDate != currentRange.StartDate || selectedRange.EndDate != currentRange.EndDate;
    }

    private void UpdatePeriodPresentation()
    {
        SelectedPeriodLabel = ReportPeriodHelper.FormatLabel(SelectedPeriod, SelectedDate);
        CurrentPeriodButtonText = ReportPeriodHelper.FormatNavigationCaption(SelectedPeriod);
        ShowDailyTrendChart = SelectedPeriod != ReportPeriod.Day;
        CommandManager.InvalidateRequerySuggested();
    }

    private void UiTimer_Tick(object? sender, EventArgs e)
    {
        _sessionTime = _sessionTime.Add(TimeSpan.FromSeconds(1));
        SessionTimeString = _sessionTime.ToString(@"hh\:mm\:ss");
    }

    private async void SyncTimer_Tick(object? sender, EventArgs e)
    {
        await TriggerSyncAsync("timer");
    }

    private void Tracker_OnWindowTracked(TrackedWindowInfo trackedWindow)
    {
        _uiDispatcher.Invoke(() =>
        {
            if (trackedWindow.IsReflineOwnedWindow)
            {
                var pausedResult = _activityBusinessServer.PauseTrackingForServiceWindow(
                    $"Статус: служебное окно Refline не учитывается ({trackedWindow.IgnoreReason})");
                if (pausedResult.IsSuccess && pausedResult.Value != null)
                {
                    StatusText = pausedResult.Value.StatusText;
                    ApplyDashboardSummary(pausedResult.Value.Summary);
                }

                CurrentTrackedAppText = string.IsNullOrWhiteSpace(trackedWindow.ProcessName) ? "Refline" : trackedWindow.ProcessName;
                CurrentTrackedWindowText = trackedWindow.WindowTitle;
                CurrentTrackedCategoryText = "Не отслеживается";
                CurrentClassificationSourceText = "Исключено из трекинга";
                CurrentMatchedRuleText = trackedWindow.IgnoreReason ?? "Служебное окно продукта";
                return;
            }

            var tickResult = _activityBusinessServer.ProcessWindowActivity(trackedWindow.WindowTitle, trackedWindow.IsIdle, DateTime.Now);
            if (!tickResult.IsSuccess || tickResult.Value == null)
            {
                StatusText = "Статус: ошибка обработки активности";
                return;
            }

            StatusText = tickResult.Value.StatusText;
            var updatedActivity = tickResult.Value.UpdatedActivity;

            if (updatedActivity != null && IsTodayDaySelection())
            {
                UpsertReportActivity(updatedActivity, tickResult.Value.IsNewActivity);
            }

            ApplyCurrentActivityDiagnostics(updatedActivity, trackedWindow);

            ApplyDashboardSummary(tickResult.Value.Summary);

            if (IsTodayDaySelection())
            {
                ApplyReportSummary(tickResult.Value.Summary);
                return;
            }

            if (GetSelectedRange().Contains(DateTime.Today))
            {
                QueueReportRefresh();
            }
        });
    }

    private void RefreshDashboardSummary()
    {
        var summaryResult = _activityBusinessServer.GetTodaySummary();
        if (summaryResult.IsSuccess && summaryResult.Value != null)
        {
            ApplyDashboardSummary(summaryResult.Value);
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
    }

    private void ApplyDashboardSummary(ActivitySummary summary)
    {
        TodayTotalString = summary.TodayTotalString;
        MostActiveAppName = summary.MostActiveAppName;
        TotalTrackedTimeText = FormatDuration(summary.Metrics.TotalTrackedSeconds);
        ActiveTimeText = FormatDuration(summary.Metrics.ActiveSeconds);
        IdleTimeText = FormatDuration(summary.Metrics.IdleSeconds);
        ProductiveTimeText = FormatDuration(summary.Metrics.ProductiveSeconds);
        TopApplicationText = FormatTopApplication(summary.Metrics.TopApplicationName);
        TopCategoryText = ToCategoryDisplayName(summary.Metrics.TopCategory);
    }

    private async void QueueReportRefresh()
    {
        if (_isRefreshingReportData)
        {
            _hasPendingReportRefresh = true;
            return;
        }

        _isRefreshingReportData = true;

        try
        {
            do
            {
                _hasPendingReportRefresh = false;
                var period = SelectedPeriod;
                var selectedDate = SelectedDate;

                var snapshot = await Task.Run(() => BuildReportSnapshot(period, selectedDate));
                if (snapshot == null)
                {
                    continue;
                }

                if (SelectedPeriod != period || SelectedDate.Date != selectedDate.Date)
                {
                    _hasPendingReportRefresh = true;
                    continue;
                }

                ApplyReportSnapshot(snapshot);
            }
            while (_hasPendingReportRefresh);
        }
        finally
        {
            _isRefreshingReportData = false;
        }
    }

    private ReportSnapshot? BuildReportSnapshot(ReportPeriod period, DateTime selectedDate)
    {
        var range = ReportPeriodHelper.GetRange(period, selectedDate);
        var reportDataResult = _activityBusinessServer.GetReportData(range.StartDate, range.EndDate);

        if (!reportDataResult.IsSuccess || reportDataResult.Value == null)
        {
            _uiDispatcher.Invoke(() =>
            {
                ClearReportState();
                MessageBox.Show(reportDataResult.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            });
            return null;
        }

        return new ReportSnapshot(
            period,
            selectedDate.Date,
            range,
            reportDataResult.Value.Activities,
            reportDataResult.Value.Summary,
            reportDataResult.Value.DailyBuckets);
    }

    private void ApplyReportSnapshot(ReportSnapshot snapshot)
    {
        ReportActivities = new ObservableCollection<AppActivity>(snapshot.Activities);
        ApplyReportSummary(snapshot.Summary);
        ApplyReportCharts(snapshot.Period, snapshot.Summary.Metrics, snapshot.DailyBuckets);
    }

    private void ApplyReportSummary(ActivitySummary summary)
    {
        ReportTotalTrackedTimeText = FormatDuration(summary.Metrics.TotalTrackedSeconds);
        ReportActiveTimeText = FormatDuration(summary.Metrics.ActiveSeconds);
        ReportIdleTimeText = FormatDuration(summary.Metrics.IdleSeconds);
        ReportProductiveTimeText = FormatDuration(summary.Metrics.ProductiveSeconds);
        ReportTopApplicationText = FormatTopApplication(summary.Metrics.TopApplicationName);
        ReportTopCategoryText = ToCategoryDisplayName(summary.Metrics.TopCategory);
    }

    private void UpsertReportActivity(AppActivity updatedActivity, bool isNew)
    {
        if (isNew)
        {
            ReportActivities.Add(updatedActivity);
        }
        else
        {
            var existing = ReportActivities.FirstOrDefault(activity =>
                string.Equals(activity.AppName, updatedActivity.AppName, StringComparison.OrdinalIgnoreCase));

            if (existing != null)
            {
                existing.TimeSpentSeconds = updatedActivity.TimeSpentSeconds;
                existing.LastActive = updatedActivity.LastActive;
                existing.WindowTitle = updatedActivity.WindowTitle;
                existing.Category = updatedActivity.Category;
                existing.ClassificationSource = updatedActivity.ClassificationSource;
                existing.MatchedRuleId = updatedActivity.MatchedRuleId;
                existing.MatchedRuleDescription = updatedActivity.MatchedRuleDescription;
                existing.IsIdle = updatedActivity.IsIdle;
                existing.IsProductive = updatedActivity.IsProductive;
                existing.Version = updatedActivity.Version;
            }
            else
            {
                ReportActivities.Add(updatedActivity);
            }
        }

        ReportActivities = new ObservableCollection<AppActivity>(ReportActivities
            .OrderByDescending(activity => activity.TimeSpentSeconds)
            .ThenBy(activity => activity.AppName, StringComparer.OrdinalIgnoreCase));
    }

    private void ApplyCurrentActivityDiagnostics(AppActivity? updatedActivity, TrackedWindowInfo trackedWindow)
    {
        if (trackedWindow.IsIdle)
        {
            CurrentTrackedAppText = "Простой";
            CurrentTrackedWindowText = trackedWindow.WindowTitle;
            CurrentTrackedCategoryText = "Система";
            CurrentClassificationSourceText = "Встроенное правило";
            CurrentMatchedRuleText = "Idle timeout";
            return;
        }

        if (updatedActivity == null)
        {
            CurrentTrackedAppText = string.IsNullOrWhiteSpace(trackedWindow.ProcessName) ? "Нет данных" : trackedWindow.ProcessName;
            CurrentTrackedWindowText = trackedWindow.WindowTitle;
            CurrentTrackedCategoryText = "Нет данных";
            CurrentClassificationSourceText = "Нет данных";
            CurrentMatchedRuleText = "—";
            return;
        }

        CurrentTrackedAppText = updatedActivity.AppName;
        CurrentTrackedWindowText = updatedActivity.WindowTitle;
        CurrentTrackedCategoryText = updatedActivity.CategoryDisplay;
        CurrentClassificationSourceText = updatedActivity.CategorySourceDisplay;
        CurrentMatchedRuleText = updatedActivity.MatchedRuleDisplay;
    }

    private void ApplyReportCharts(
        ReportPeriod period,
        ActivityMetricsSummary metrics,
        IReadOnlyList<ActivityDailyBucket> dailyBuckets)
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
            .Take(7)
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
                MaxBarWidth = 42
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
        TopApplicationsYAxes = CreateDurationAxes();

        ApplyDailyTrendChart(period, dailyBuckets);
    }

    private void ApplyDailyTrendChart(ReportPeriod period, IReadOnlyList<ActivityDailyBucket> dailyBuckets)
    {
        ShowDailyTrendChart = period != ReportPeriod.Day;
        if (!ShowDailyTrendChart)
        {
            HasDailyTrendChartData = false;
            DailyTrendChartPlaceholderText = "График по дням доступен для недели и месяца";
            DailyTrendSeries = Array.Empty<ISeries>();
            DailyTrendXAxes = CreateChartLabelAxis(new[] { "Нет данных" });
            DailyTrendYAxes = CreateDurationAxes();
            return;
        }

        var labels = dailyBuckets.Select(bucket => FormatBucketLabel(period, bucket.Date)).ToArray();
        var totalValues = dailyBuckets.Select(bucket => bucket.TotalTrackedSeconds).ToArray();
        var productiveValues = dailyBuckets.Select(bucket => bucket.ProductiveSeconds).ToArray();
        var hasValues = totalValues.Any(value => value > 0) || productiveValues.Any(value => value > 0);

        HasDailyTrendChartData = hasValues;
        DailyTrendChartPlaceholderText = "Нет данных за выбранный период";

        DailyTrendSeries = hasValues
            ? new ISeries[]
            {
                new ColumnSeries<int>
                {
                    Name = "Общее время",
                    Values = totalValues,
                    Fill = new SolidColorPaint(new SKColor(45, 199, 255)),
                    Stroke = null,
                    MaxBarWidth = 32
                },
                new LineSeries<int>
                {
                    Name = "Продуктивное время",
                    Values = productiveValues,
                    Stroke = new SolidColorPaint(new SKColor(0, 255, 200)) { StrokeThickness = 3 },
                    Fill = null,
                    GeometrySize = 10,
                    GeometryFill = new SolidColorPaint(new SKColor(0, 255, 200)),
                    GeometryStroke = null
                }
            }
            : Array.Empty<ISeries>();

        DailyTrendXAxes = CreateChartLabelAxis(labels.Length == 0 ? new[] { "Нет данных" } : labels);
        DailyTrendYAxes = CreateDurationAxes();
    }

    private void ClearReportState()
    {
        ReportActivities = new ObservableCollection<AppActivity>();
        ReportTotalTrackedTimeText = "00:00:00";
        ReportActiveTimeText = "00:00:00";
        ReportIdleTimeText = "00:00:00";
        ReportProductiveTimeText = "00:00:00";
        ReportTopApplicationText = "Нет данных";
        ReportTopCategoryText = "Нет данных";
        CurrentTrackedAppText = "Нет данных";
        CurrentTrackedWindowText = "Нет данных";
        CurrentTrackedCategoryText = "Нет данных";
        CurrentClassificationSourceText = "Нет данных";
        CurrentMatchedRuleText = "—";
        ApplyEmptyCharts();
    }

    private void ApplyEmptyCharts()
    {
        HasCategoryChartData = false;
        CategoryChartPlaceholderText = "Категории пока не определены";
        CategoryTimeSeries = Array.Empty<ISeries>();
        TopApplicationsSeries = Array.Empty<ISeries>();
        TopApplicationsXAxes = CreateChartLabelAxis(new[] { "Нет данных" });
        TopApplicationsYAxes = CreateDurationAxes();

        HasDailyTrendChartData = false;
        DailyTrendChartPlaceholderText = ShowDailyTrendChart
            ? "Нет данных за выбранный период"
            : "График по дням доступен для недели и месяца";
        DailyTrendSeries = Array.Empty<ISeries>();
        DailyTrendXAxes = CreateChartLabelAxis(new[] { "Нет данных" });
        DailyTrendYAxes = CreateDurationAxes();
    }

    private bool IsTodayDaySelection()
    {
        return SelectedPeriod == ReportPeriod.Day && SelectedDate.Date == DateTime.Today;
    }

    private ReportPeriodRange GetSelectedRange()
    {
        return ReportPeriodHelper.GetRange(SelectedPeriod, SelectedDate);
    }

    private static bool TryParseReportPeriod(object? parameter, out ReportPeriod period)
    {
        if (parameter is ReportPeriod directPeriod)
        {
            period = directPeriod;
            return true;
        }

        return Enum.TryParse(parameter?.ToString(), true, out period);
    }

    private static string FormatTopApplication(string value)
    {
        return string.IsNullOrWhiteSpace(value) || value == "—"
            ? "Нет данных"
            : value;
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

    private static string TrimChartLabel(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "Нет данных";
        }

        var trimmed = value.Trim();
        return trimmed.Length > 18 ? trimmed[..18] + "..." : trimmed;
    }

    private static string FormatBucketLabel(ReportPeriod period, DateTime date)
    {
        return period == ReportPeriod.Week
            ? $"{RussianCulture.DateTimeFormat.GetShortestDayName(date.DayOfWeek)} {date:dd.MM}"
            : date.ToString("dd.MM", RussianCulture);
    }

    private static Axis[] CreateChartLabelAxis(string[] labels)
    {
        return new[]
        {
            new Axis
            {
                Labels = labels,
                LabelsPaint = new SolidColorPaint(new SKColor(155, 174, 194)),
                TextSize = 12,
                SeparatorsPaint = new SolidColorPaint(new SKColor(31, 41, 55))
            }
        };
    }

    private static Axis[] CreateDurationAxes()
    {
        return new[]
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
        _syncTimer.Stop();

        if (IsTracking)
        {
            _windowTracker.Stop();
        }

        var saveResult = _activityBusinessServer.SaveCurrentSession();
        if (!saveResult.IsSuccess)
        {
            AppLogger.Log(saveResult.Message, "ERROR");
        }

        try
        {
            var syncResult = _activitySyncService.TrySyncPendingAsync().GetAwaiter().GetResult();
            if (!syncResult.IsSuccess)
            {
                AppLogger.Log(syncResult.Message, "ERROR");
            }
        }
        catch (Exception ex)
        {
            AppLogger.Log($"Ошибка финальной activity sync: {ex.Message}", "ERROR");
        }
    }

    private async Task TriggerSyncAsync(string trigger)
    {
        try
        {
            var syncResult = await _activitySyncService.TrySyncPendingAsync();
            if (syncResult.IsSuccess)
            {
                if (syncResult.Value > 0)
                {
                    AppLogger.Log($"Activity sync trigger '{trigger}' completed. {syncResult.Message}");
                }

                return;
            }

            AppLogger.Log($"Activity sync trigger '{trigger}' failed. {syncResult.Message}", "ERROR");
        }
        catch (Exception ex)
        {
            AppLogger.Log($"Ошибка activity sync trigger '{trigger}': {ex.Message}", "ERROR");
        }
    }

    private sealed record ReportSnapshot(
        ReportPeriod Period,
        DateTime SelectedDate,
        ReportPeriodRange Range,
        IReadOnlyList<AppActivity> Activities,
        ActivitySummary Summary,
        IReadOnlyList<ActivityDailyBucket> DailyBuckets);
}
