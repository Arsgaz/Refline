using System.Collections.ObjectModel;
using System.Windows.Input;
using Refline.Admin.Business.Identity;
using Refline.Admin.Models;
using Refline.Admin.Services.Api;
using Refline.Admin.Utils;

namespace Refline.Admin.ViewModels;

public sealed class TeamDashboardViewModel : ViewModelBase
{
    private readonly ITeamDashboardService _dashboardService;
    private readonly CurrentSessionContext _currentSessionContext;

    private string _errorMessage = string.Empty;
    private bool _isLoading;
    private bool _hasAnyData;
    private DateTime _referenceDate;
    private DateTime _customStartDate;
    private DateTime _customEndDate;
    private AnalyticsPeriodKind _selectedPeriodKind;
    private string _periodLabel = string.Empty;
    private int _employeesCount;
    private int _totalTrackedSeconds;
    private int _totalProductiveSeconds;
    private int _totalIdleSeconds;
    private int _averageProductiveSecondsPerEmployee;
    private string _topEmployeeName = "Нет данных";
    private string _topEmployeeMetric = "Нет данных";

    public TeamDashboardViewModel(
        ITeamDashboardService dashboardService,
        CurrentSessionContext currentSessionContext)
    {
        _dashboardService = dashboardService;
        _currentSessionContext = currentSessionContext;
        _referenceDate = DateTime.Today;
        _customStartDate = DateTime.Today.AddDays(-7);
        _customEndDate = DateTime.Today;
        _selectedPeriodKind = AnalyticsPeriodKind.Week;

        DailyBuckets = new ObservableCollection<TeamDayBarViewModel>();
        TopEmployees = new ObservableCollection<TeamMemberListItemViewModel>();
        Applications = new ObservableCollection<TeamAggregateListItemViewModel>();
        Categories = new ObservableCollection<TeamAggregateListItemViewModel>();

        LoadCommand = new RelayCommand(async () => await LoadAsync(forceReload: true), () => !IsLoading);
        PreviousPeriodCommand = new RelayCommand(async () => await ShiftPeriodAsync(-1), () => !IsLoading);
        NextPeriodCommand = new RelayCommand(async () => await ShiftPeriodAsync(1), () => !IsLoading);
        TodayCommand = new RelayCommand(async () => await MoveToTodayAsync(), () => !IsLoading);
        SetDayPeriodCommand = new RelayCommand(async () => await ChangePeriodAsync(AnalyticsPeriodKind.Day), () => !IsLoading);
        SetWeekPeriodCommand = new RelayCommand(async () => await ChangePeriodAsync(AnalyticsPeriodKind.Week), () => !IsLoading);
        SetMonthPeriodCommand = new RelayCommand(async () => await ChangePeriodAsync(AnalyticsPeriodKind.Month), () => !IsLoading);
        SetCustomPeriodCommand = new RelayCommand(async () => await ChangePeriodAsync(AnalyticsPeriodKind.Custom), () => !IsLoading);
    }

    public ObservableCollection<TeamDayBarViewModel> DailyBuckets { get; }

    public ObservableCollection<TeamMemberListItemViewModel> TopEmployees { get; }

    public ObservableCollection<TeamAggregateListItemViewModel> Applications { get; }

    public ObservableCollection<TeamAggregateListItemViewModel> Categories { get; }

    public ICommand LoadCommand { get; }

    public ICommand PreviousPeriodCommand { get; }

    public ICommand NextPeriodCommand { get; }

    public ICommand TodayCommand { get; }

    public ICommand SetDayPeriodCommand { get; }

    public ICommand SetWeekPeriodCommand { get; }

    public ICommand SetMonthPeriodCommand { get; }

    public ICommand SetCustomPeriodCommand { get; }

    public string ScopeTitle => _currentSessionContext.Role == UserRole.Manager
        ? "Сводка по команде"
        : "Сводка по компании";

    public string ScopeSubtitle => _currentSessionContext.Role == UserRole.Manager
        ? "Показывает вас и ваших сотрудников, доступных по текущей role-based модели."
        : "Показывает всех сотрудников компании, доступных администратору.";

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

    public bool IsEmptyStateVisible => !IsLoading && !HasError && !HasAnyData;

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

    public string EmployeesCountDisplay => _employeesCount.ToString();

    public string TotalTrackedDisplay => FormatDuration(_totalTrackedSeconds);

    public string TotalProductiveDisplay => FormatDuration(_totalProductiveSeconds);

    public string TotalIdleDisplay => FormatDuration(_totalIdleSeconds);

    public string AverageProductiveDisplay => FormatDuration(_averageProductiveSecondsPerEmployee);

    public string TopEmployeeName
    {
        get => _topEmployeeName;
        private set => SetProperty(ref _topEmployeeName, value);
    }

    public string TopEmployeeMetric
    {
        get => _topEmployeeMetric;
        private set => SetProperty(ref _topEmployeeMetric, value);
    }

    public async Task EnsureLoadedAsync()
    {
        if (HasAnyData || HasError || IsLoading)
        {
            return;
        }

        await LoadAsync();
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
        if (IsLoading)
        {
            return;
        }

        ErrorMessage = string.Empty;
        IsLoading = true;

        try
        {
            var (from, to) = GetRange();
            PeriodLabel = $"{from:dd.MM.yyyy} - {to:dd.MM.yyyy}";

            var result = await _dashboardService.GetDashboardAsync(from, to);
            if (!result.IsSuccess || result.Value is null)
            {
                ResetState();
                ErrorMessage = string.IsNullOrWhiteSpace(result.Message)
                    ? "Не удалось загрузить сводную аналитику."
                    : result.Message;
                return;
            }

            ApplySnapshot(result.Value, from, to);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ApplySnapshot(TeamDashboardSnapshot snapshot, DateOnly from, DateOnly to)
    {
        _employeesCount = snapshot.Users.Count;
        _totalTrackedSeconds = snapshot.TotalTrackedSeconds;
        _totalProductiveSeconds = snapshot.TotalProductiveSeconds;
        _totalIdleSeconds = snapshot.TotalIdleSeconds;
        _averageProductiveSecondsPerEmployee = snapshot.AverageProductiveSecondsPerEmployee;

        TopEmployeeName = snapshot.TopEmployeeByProductiveTime?.User.FullName ?? "Нет данных";
        TopEmployeeMetric = snapshot.TopEmployeeByProductiveTime is null
            ? "Нет данных"
            : $"{FormatDuration(snapshot.TopEmployeeByProductiveTime.ProductiveSeconds)} продуктивного времени";

        OnPropertyChanged(nameof(EmployeesCountDisplay));
        OnPropertyChanged(nameof(TotalTrackedDisplay));
        OnPropertyChanged(nameof(TotalProductiveDisplay));
        OnPropertyChanged(nameof(TotalIdleDisplay));
        OnPropertyChanged(nameof(AverageProductiveDisplay));

        TopEmployees.Clear();
        foreach (var member in snapshot.Members.Take(8))
        {
            TopEmployees.Add(new TeamMemberListItemViewModel(
                member.User.FullName,
                member.User.RoleDisplay,
                FormatDuration(member.ProductiveSeconds),
                FormatDuration(member.TotalTrackedSeconds)));
        }

        Applications.Clear();
        foreach (var app in snapshot.Applications)
        {
            Applications.Add(new TeamAggregateListItemViewModel(app.Name, FormatDuration(app.TotalSeconds)));
        }

        Categories.Clear();
        foreach (var category in snapshot.Categories)
        {
            Categories.Add(new TeamAggregateListItemViewModel(category.Name, FormatDuration(category.TotalSeconds)));
        }

        BuildDailyBuckets(snapshot.Days, from, to);
        HasAnyData = snapshot.TotalTrackedSeconds > 0 || snapshot.Users.Count > 0;
    }

    private void BuildDailyBuckets(IReadOnlyList<TeamDailyAggregate> days, DateOnly from, DateOnly to)
    {
        DailyBuckets.Clear();

        var rangeDays = Enumerable.Range(0, to.DayNumber - from.DayNumber + 1)
            .Select(offset => from.AddDays(offset))
            .ToList();

        var dayMap = days.ToDictionary(item => item.Date);
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

            DailyBuckets.Add(new TeamDayBarViewModel(
                day.ToString("dd.MM"),
                FormatDuration(totalSeconds),
                12 + (112.0 * totalSeconds / maxTotalSeconds),
                productiveSeconds,
                idleSeconds));
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

    private void ResetState()
    {
        _employeesCount = 0;
        _totalTrackedSeconds = 0;
        _totalProductiveSeconds = 0;
        _totalIdleSeconds = 0;
        _averageProductiveSecondsPerEmployee = 0;
        TopEmployeeName = "Нет данных";
        TopEmployeeMetric = "Нет данных";
        DailyBuckets.Clear();
        TopEmployees.Clear();
        Applications.Clear();
        Categories.Clear();
        HasAnyData = false;

        OnPropertyChanged(nameof(EmployeesCountDisplay));
        OnPropertyChanged(nameof(TotalTrackedDisplay));
        OnPropertyChanged(nameof(TotalProductiveDisplay));
        OnPropertyChanged(nameof(TotalIdleDisplay));
        OnPropertyChanged(nameof(AverageProductiveDisplay));
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

public sealed record TeamDayBarViewModel(
    string Label,
    string TotalDisplay,
    double BarHeight,
    int ProductiveSeconds,
    int IdleSeconds);

public sealed record TeamMemberListItemViewModel(
    string FullName,
    string Role,
    string ProductiveDisplay,
    string TotalDisplay);

public sealed record TeamAggregateListItemViewModel(
    string Name,
    string DurationDisplay);
