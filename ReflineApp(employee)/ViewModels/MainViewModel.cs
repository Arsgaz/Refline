using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Refline.Business.Activity;
using Refline.Business.Reports;
using Refline.Models;
using Refline.Services;
using Refline.Utils;

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

    public ICommand ToggleTrackingCommand { get; }
    public ICommand ExportCommand { get; }

    private void LoadInitialData()
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
    }

    private void ApplySummary(ActivitySummary summary)
    {
        TodayTotalString = summary.TodayTotalString;
        MostActiveAppName = summary.MostActiveAppName;
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
