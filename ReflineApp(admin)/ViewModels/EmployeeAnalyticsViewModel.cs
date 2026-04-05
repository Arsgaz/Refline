using System.Collections.ObjectModel;
using System.Windows.Input;
using Refline.Admin.Models;
using Refline.Admin.Services.Api;
using Refline.Admin.Utils;

namespace Refline.Admin.ViewModels;

public sealed class EmployeeAnalyticsViewModel : ViewModelBase
{
    private readonly IAdminUserAnalyticsService _analyticsService;
    private readonly Action _navigateBack;

    private CompanyUserListItem? _employee;
    private string _errorMessage = string.Empty;
    private bool _isLoading;
    private DateTime _referenceDate;
    private DateTime _customStartDate;
    private DateTime _customEndDate;
    private AnalyticsPeriodKind _selectedPeriodKind;
    private int _totalTrackedSeconds;
    private int _activeSeconds;
    private int _idleSeconds;
    private int _productiveSeconds;
    private string _topApplicationName = "Нет данных";
    private string _topCategory = "Нет данных";
    private string _periodLabel = string.Empty;
    private bool _hasAnyData;

    public EmployeeAnalyticsViewModel(
        IAdminUserAnalyticsService analyticsService,
        Action navigateBack)
    {
        _analyticsService = analyticsService;
        _navigateBack = navigateBack;
        _referenceDate = DateTime.Today;
        _customStartDate = DateTime.Today.AddDays(-7);
        _customEndDate = DateTime.Today;
        _selectedPeriodKind = AnalyticsPeriodKind.Week;

        Applications = new ObservableCollection<AnalyticsListItemViewModel>();
        Categories = new ObservableCollection<AnalyticsListItemViewModel>();
        DailyBuckets = new ObservableCollection<AnalyticsDayBarViewModel>();

        LoadCommand = new RelayCommand(async () => await LoadAsync(forceReload: true), () => !IsLoading && Employee is not null);
        PreviousPeriodCommand = new RelayCommand(async () => await ShiftPeriodAsync(-1), () => !IsLoading && Employee is not null);
        NextPeriodCommand = new RelayCommand(async () => await ShiftPeriodAsync(1), () => !IsLoading && Employee is not null);
        TodayCommand = new RelayCommand(async () => await MoveToTodayAsync(), () => !IsLoading && Employee is not null);
        SetDayPeriodCommand = new RelayCommand(async () => await ChangePeriodAsync(AnalyticsPeriodKind.Day), () => !IsLoading && Employee is not null);
        SetWeekPeriodCommand = new RelayCommand(async () => await ChangePeriodAsync(AnalyticsPeriodKind.Week), () => !IsLoading && Employee is not null);
        SetMonthPeriodCommand = new RelayCommand(async () => await ChangePeriodAsync(AnalyticsPeriodKind.Month), () => !IsLoading && Employee is not null);
        SetCustomPeriodCommand = new RelayCommand(async () => await ChangePeriodAsync(AnalyticsPeriodKind.Custom), () => !IsLoading && Employee is not null);
        BackCommand = new RelayCommand(_navigateBack);
    }

    public ObservableCollection<AnalyticsListItemViewModel> Applications { get; }

    public ObservableCollection<AnalyticsListItemViewModel> Categories { get; }

    public ObservableCollection<AnalyticsDayBarViewModel> DailyBuckets { get; }

    public ICommand LoadCommand { get; }

    public ICommand PreviousPeriodCommand { get; }

    public ICommand NextPeriodCommand { get; }

    public ICommand TodayCommand { get; }

    public ICommand SetDayPeriodCommand { get; }

    public ICommand SetWeekPeriodCommand { get; }

    public ICommand SetMonthPeriodCommand { get; }

    public ICommand SetCustomPeriodCommand { get; }

    public ICommand BackCommand { get; }

    public CompanyUserListItem? Employee
    {
        get => _employee;
        private set
        {
            if (SetProperty(ref _employee, value))
            {
                OnPropertyChanged(nameof(EmployeeHeader));
                OnPropertyChanged(nameof(EmployeeSubheader));
                OnPropertyChanged(nameof(EmployeeStatusDisplay));
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public string EmployeeHeader => Employee?.FullName ?? "Сотрудник не выбран";

    public string EmployeeSubheader => Employee is null
        ? "Выберите сотрудника из списка."
        : $"{Employee.Login} · {Employee.RoleDisplay}";

    public string EmployeeStatusDisplay => Employee?.StatusDisplay ?? "Нет данных";

    public string ErrorMessage
    {
        get => _errorMessage;
        private set
        {
            if (SetProperty(ref _errorMessage, value))
            {
                OnPropertyChanged(nameof(HasError));
                OnPropertyChanged(nameof(IsEmptyStateVisible));
            }
        }
    }

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public bool IsLoading
    {
        get => _isLoading;
        private set
        {
            if (SetProperty(ref _isLoading, value))
            {
                OnPropertyChanged(nameof(IsEmptyStateVisible));
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public bool HasAnyData
    {
        get => _hasAnyData;
        private set
        {
            if (SetProperty(ref _hasAnyData, value))
            {
                OnPropertyChanged(nameof(IsEmptyStateVisible));
            }
        }
    }

    public bool IsEmptyStateVisible => Employee is not null && !IsLoading && !HasError && !HasAnyData;

    public DateTime ReferenceDate
    {
        get => _referenceDate;
        set
        {
            if (SetProperty(ref _referenceDate, value))
            {
                OnPropertyChanged(nameof(ReferenceDateDisplay));
                if (SelectedPeriodKind != AnalyticsPeriodKind.Custom) 
                {
                    _ = LoadAsync(forceReload: true);
                }
            }
        }
    }

    public string ReferenceDateDisplay => ReferenceDate.ToString("dd.MM.yyyy");

    public DateTime CustomStartDate
    {
        get => _customStartDate;
        set
        {
            if (SetProperty(ref _customStartDate, value) && SelectedPeriodKind == AnalyticsPeriodKind.Custom)
            {
                _ = LoadAsync(forceReload: true);
            }
        }
    }

    public DateTime CustomEndDate
    {
        get => _customEndDate;
        set
        {
            if (SetProperty(ref _customEndDate, value) && SelectedPeriodKind == AnalyticsPeriodKind.Custom)
            {
                _ = LoadAsync(forceReload: true);
            }
        }
    }

    public AnalyticsPeriodKind SelectedPeriodKind
    {
        get => _selectedPeriodKind;
        private set
        {
            if (SetProperty(ref _selectedPeriodKind, value))
            {
                OnPropertyChanged(nameof(IsDayPeriodSelected));
                OnPropertyChanged(nameof(IsWeekPeriodSelected));
                OnPropertyChanged(nameof(IsMonthPeriodSelected));
                OnPropertyChanged(nameof(IsCustomPeriodSelected));
                OnPropertyChanged(nameof(IsReferenceDateVisible));
                OnPropertyChanged(nameof(IsCustomDatesVisible));
            }
        }
    }

    public bool IsDayPeriodSelected => SelectedPeriodKind == AnalyticsPeriodKind.Day;

    public bool IsWeekPeriodSelected => SelectedPeriodKind == AnalyticsPeriodKind.Week;

    public bool IsMonthPeriodSelected => SelectedPeriodKind == AnalyticsPeriodKind.Month;

    public bool IsCustomPeriodSelected => SelectedPeriodKind == AnalyticsPeriodKind.Custom;

    public bool IsReferenceDateVisible => SelectedPeriodKind != AnalyticsPeriodKind.Custom;

    public bool IsCustomDatesVisible => SelectedPeriodKind == AnalyticsPeriodKind.Custom;

    public string PeriodLabel
    {
        get => _periodLabel;
        private set => SetProperty(ref _periodLabel, value);
    }

    public string TotalTrackedDisplay => FormatDuration(_totalTrackedSeconds);

    public string ActiveDisplay => FormatDuration(_activeSeconds);

    public string IdleDisplay => FormatDuration(_idleSeconds);

    public string ProductiveDisplay => FormatDuration(_productiveSeconds);

    public string TopApplicationName
    {
        get => _topApplicationName;
        private set => SetProperty(ref _topApplicationName, value);
    }

    public string TopCategory
    {
        get => _topCategory;
        private set => SetProperty(ref _topCategory, value);
    }

    public async Task OpenForEmployeeAsync(CompanyUserListItem employee)
    {
        Employee = employee;
        await LoadAsync(forceReload: true);
    }

    private async Task ShiftPeriodAsync(int direction)
    {
        if (SelectedPeriodKind == AnalyticsPeriodKind.Custom)
        {
            var diff = (CustomEndDate - CustomStartDate).Days + 1;
            CustomStartDate = CustomStartDate.AddDays(diff * direction);
            CustomEndDate = CustomEndDate.AddDays(diff * direction);
        }
        else
        {
            ReferenceDate = SelectedPeriodKind switch
            {
                AnalyticsPeriodKind.Day => ReferenceDate.AddDays(direction),
                AnalyticsPeriodKind.Week => ReferenceDate.AddDays(7 * direction),
                _ => ReferenceDate.AddMonths(direction)
            };
        }

        await LoadAsync(forceReload: true);
    }

    private async Task MoveToTodayAsync()
    {
        if (SelectedPeriodKind == AnalyticsPeriodKind.Custom)
        {
            var diff = (CustomEndDate - CustomStartDate).Days;
            CustomEndDate = DateTime.Today;
            CustomStartDate = DateTime.Today.AddDays(-diff);
        }
        else
        {
            ReferenceDate = DateTime.Today;
        }
        await LoadAsync(forceReload: true);
    }

    private async Task ChangePeriodAsync(AnalyticsPeriodKind periodKind)
    {
        if (SelectedPeriodKind == periodKind)
        {
            return;
        }

        SelectedPeriodKind = periodKind;
        await LoadAsync(forceReload: true);
    }

    public async Task LoadAsync(bool forceReload = false)
    {
        if (Employee is null || IsLoading)
        {
            return;
        }

        ErrorMessage = string.Empty;
        IsLoading = true;

        try
        {
            var (from, to) = GetRange();
            PeriodLabel = BuildPeriodLabel(from, to);

            var result = await _analyticsService.GetEmployeeAnalyticsAsync(Employee.Id, from, to);
            if (!result.IsSuccess || result.Value is null)
            {
                ResetAnalytics();
                ErrorMessage = string.IsNullOrWhiteSpace(result.Message)
                    ? "Не удалось загрузить аналитику сотрудника."
                    : result.Message;
                return;
            }

            ApplyAnalytics(result.Value, from, to);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ApplyAnalytics(EmployeeAnalyticsSnapshot snapshot, DateOnly from, DateOnly to)
    {
        _totalTrackedSeconds = snapshot.Summary.TotalTrackedSeconds;
        _activeSeconds = snapshot.Summary.ActiveSeconds;
        _idleSeconds = snapshot.Summary.IdleSeconds;
        _productiveSeconds = snapshot.Summary.ProductiveSeconds;
        TopApplicationName = string.IsNullOrWhiteSpace(snapshot.Summary.TopApplicationName)
            ? "Нет данных"
            : snapshot.Summary.TopApplicationName;
        TopCategory = string.IsNullOrWhiteSpace(snapshot.Summary.TopCategory)
            ? "Нет данных"
            : snapshot.Summary.TopCategory;

        OnPropertyChanged(nameof(TotalTrackedDisplay));
        OnPropertyChanged(nameof(ActiveDisplay));
        OnPropertyChanged(nameof(IdleDisplay));
        OnPropertyChanged(nameof(ProductiveDisplay));

        Applications.Clear();
        foreach (var app in snapshot.Breakdown.Applications
                     .OrderByDescending(item => item.TotalSeconds)
                     .Take(8))
        {
            Applications.Add(new AnalyticsListItemViewModel(
                app.ApplicationName,
                app.TotalSeconds,
                FormatDuration(app.TotalSeconds)));
        }

        Categories.Clear();
        foreach (var category in snapshot.Breakdown.Categories
                     .OrderByDescending(item => item.TotalSeconds)
                     .Take(8))
        {
            Categories.Add(new AnalyticsListItemViewModel(
                category.Category,
                category.TotalSeconds,
                FormatDuration(category.TotalSeconds)));
        }

        BuildDailyBuckets(snapshot.Breakdown.Days, from, to);

        HasAnyData = snapshot.Summary.TotalRecordsCount > 0;
    }

    private void BuildDailyBuckets(IReadOnlyList<UserActivityDay> apiDays, DateOnly from, DateOnly to)
    {
        DailyBuckets.Clear();

        var rangeDays = Enumerable.Range(0, to.DayNumber - from.DayNumber + 1)
            .Select(offset => from.AddDays(offset))
            .ToList();

        var dayMap = apiDays.ToDictionary(day => day.Date);
        var maxTotalSeconds = Math.Max(1, rangeDays
            .Select(day => dayMap.TryGetValue(day, out var value) ? value.TotalSeconds : 0)
            .DefaultIfEmpty(0)
            .Max());

        foreach (var day in rangeDays)
        {
            dayMap.TryGetValue(day, out var value);
            var totalSeconds = value?.TotalSeconds ?? 0;
            var productiveSeconds = value?.ProductiveSeconds ?? 0;
            var idleSeconds = value?.IdleSeconds ?? 0;

            DailyBuckets.Add(new AnalyticsDayBarViewModel(
                day.ToString("dd.MM"),
                totalSeconds,
                productiveSeconds,
                idleSeconds,
                12 + (112.0 * totalSeconds / maxTotalSeconds),
                FormatDuration(totalSeconds)));
        }
    }

    private (DateOnly From, DateOnly To) GetRange()
    {
        if (SelectedPeriodKind == AnalyticsPeriodKind.Custom) 
        {
            var customStart = DateOnly.FromDateTime(CustomStartDate);
            var customEnd = DateOnly.FromDateTime(CustomEndDate);
            return customStart <= customEnd ? (customStart, customEnd) : (customEnd, customStart);
        }

        var baseDate = DateOnly.FromDateTime(ReferenceDate);

        return SelectedPeriodKind switch
        {
            AnalyticsPeriodKind.Day => (baseDate, baseDate),
            AnalyticsPeriodKind.Week => (baseDate.AddDays(-6), baseDate),
            _ => (
                new DateOnly(baseDate.Year, baseDate.Month, 1),
                new DateOnly(baseDate.Year, baseDate.Month, DateTime.DaysInMonth(baseDate.Year, baseDate.Month)))
        };
    }

    private string BuildPeriodLabel(DateOnly from, DateOnly to)
    {
        return $"{from:dd.MM.yyyy} - {to:dd.MM.yyyy}";
    }

    private void ResetAnalytics()
    {
        _totalTrackedSeconds = 0;
        _activeSeconds = 0;
        _idleSeconds = 0;
        _productiveSeconds = 0;
        TopApplicationName = "Нет данных";
        TopCategory = "Нет данных";
        Applications.Clear();
        Categories.Clear();
        DailyBuckets.Clear();
        HasAnyData = false;

        OnPropertyChanged(nameof(TotalTrackedDisplay));
        OnPropertyChanged(nameof(ActiveDisplay));
        OnPropertyChanged(nameof(IdleDisplay));
        OnPropertyChanged(nameof(ProductiveDisplay));
    }

    private static string FormatDuration(int totalSeconds)
    {
        var timeSpan = TimeSpan.FromSeconds(totalSeconds);
        if (timeSpan.TotalHours >= 1)
        {
            return $"{(int)timeSpan.TotalHours} ч {timeSpan.Minutes:D2} мин";
        }

        if (timeSpan.TotalMinutes >= 1)
        {
            return $"{timeSpan.Minutes} мин";
        }

        return $"{timeSpan.Seconds} сек";
    }
}

public sealed record AnalyticsListItemViewModel(
    string Name,
    int TotalSeconds,
    string DurationDisplay);

public sealed record AnalyticsDayBarViewModel(
    string Label,
    int TotalSeconds,
    int ProductiveSeconds,
    int IdleSeconds,
    double BarHeight,
    string TotalDisplay);
