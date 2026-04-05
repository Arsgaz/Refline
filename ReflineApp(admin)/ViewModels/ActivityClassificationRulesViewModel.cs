using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using Refline.Admin.Business.Identity;
using Refline.Admin.Models;
using Refline.Admin.Services.Api;
using Refline.Admin.Utils;
using Refline.Admin.Views;

namespace Refline.Admin.ViewModels;

public sealed class ActivityClassificationRulesViewModel : ViewModelBase
{
    private readonly IActivityClassificationRulesService _rulesService;
    private readonly CurrentSessionContext _currentSessionContext;
    private string _errorMessage = string.Empty;
    private bool _isLoading;
    private bool _hasLoaded;
    private ActivityClassificationRule? _selectedRule;

    public ActivityClassificationRulesViewModel(
        IActivityClassificationRulesService rulesService,
        CurrentSessionContext currentSessionContext)
    {
        _rulesService = rulesService;
        _currentSessionContext = currentSessionContext;

        Rules = new ObservableCollection<ActivityClassificationRule>();
        RefreshCommand = new RelayCommand(async () => await LoadAsync(forceReload: true), () => !IsLoading && CanManageRules);
        CreateRuleCommand = new RelayCommand(async () => await CreateRuleAsync(), () => !IsLoading && CanManageRules);
        EditRuleCommand = new RelayCommand(
            async parameter => await EditRuleAsync(parameter as ActivityClassificationRule ?? SelectedRule),
            parameter => !IsLoading && CanManageRules && (parameter as ActivityClassificationRule ?? SelectedRule) is not null);
        ToggleRuleCommand = new RelayCommand(
            async parameter => await ToggleRuleAsync(parameter as ActivityClassificationRule ?? SelectedRule),
            parameter => !IsLoading && CanManageRules && (parameter as ActivityClassificationRule ?? SelectedRule) is not null);
        DeleteRuleCommand = new RelayCommand(
            async parameter => await DeleteRuleAsync(parameter as ActivityClassificationRule ?? SelectedRule),
            parameter => !IsLoading && CanManageRules && (parameter as ActivityClassificationRule ?? SelectedRule) is not null);
    }

    public ObservableCollection<ActivityClassificationRule> Rules { get; }

    public ICommand RefreshCommand { get; }

    public ICommand CreateRuleCommand { get; }

    public ICommand EditRuleCommand { get; }

    public ICommand ToggleRuleCommand { get; }

    public ICommand DeleteRuleCommand { get; }

    public ActivityClassificationRule? SelectedRule
    {
        get => _selectedRule;
        set
        {
            if (SetProperty(ref _selectedRule, value))
            {
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

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

    public bool IsLoading
    {
        get => _isLoading;
        private set
        {
            if (SetProperty(ref _isLoading, value))
            {
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public bool CanManageRules => _currentSessionContext.Role == UserRole.Admin;

    public bool HasRules => Rules.Count > 0;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public bool IsEmptyStateVisible => !IsLoading && !HasRules && !HasError;

    public int EnabledRulesCount => Rules.Count(rule => rule.IsEnabled);

    public int DisabledRulesCount => Rules.Count(rule => !rule.IsEnabled);

    public string RulesCountText => HasRules
        ? $"Найдено правил: {Rules.Count}"
        : "Правила ещё не загружены";

    public async Task EnsureLoadedAsync()
    {
        if (_hasLoaded || !CanManageRules)
        {
            return;
        }

        await LoadAsync();
    }

    public async Task LoadAsync(bool forceReload = false)
    {
        if (!CanManageRules || IsLoading || (_hasLoaded && !forceReload))
        {
            return;
        }

        ErrorMessage = string.Empty;
        IsLoading = true;

        try
        {
            var result = await _rulesService.GetCompanyRulesAsync(_currentSessionContext.CompanyId);
            if (!result.IsSuccess || result.Value is null)
            {
                Rules.Clear();
                _hasLoaded = false;
                ErrorMessage = string.IsNullOrWhiteSpace(result.Message)
                    ? "Не удалось загрузить правила классификации."
                    : result.Message;
                RaiseSummaryChanges();
                return;
            }

            Rules.Clear();
            foreach (var rule in result.Value.OrderBy(item => item.Priority).ThenBy(item => item.Id))
            {
                Rules.Add(rule);
            }

            _hasLoaded = true;
            RaiseSummaryChanges();
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(IsEmptyStateVisible));
        }
    }

    private async Task CreateRuleAsync()
    {
        var dialogResult = ShowEditorDialog(null);
        if (dialogResult is null)
        {
            return;
        }

        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            var result = await _rulesService.CreateRuleAsync(new ActivityClassificationRuleCreateRequest
            {
                CompanyId = _currentSessionContext.CompanyId,
                AppNamePattern = dialogResult.AppNamePattern,
                WindowTitlePattern = dialogResult.WindowTitlePattern,
                Category = dialogResult.Category,
                Priority = dialogResult.Priority,
                IsEnabled = dialogResult.IsEnabled
            });

            if (!result.IsSuccess)
            {
                MessageBox.Show(
                    string.IsNullOrWhiteSpace(result.Message) ? "Не удалось создать правило." : result.Message,
                    "Ошибка создания",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            IsLoading = false;
            await LoadAsync(forceReload: true);
            MessageBox.Show(
                "Правило классификации создано.",
                "Правило создано",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task EditRuleAsync(ActivityClassificationRule? rule)
    {
        if (rule is null)
        {
            return;
        }

        var dialogResult = ShowEditorDialog(rule);
        if (dialogResult is null)
        {
            return;
        }

        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            var result = await _rulesService.UpdateRuleAsync(rule.Id, new ActivityClassificationRuleUpdateRequest
            {
                CompanyId = _currentSessionContext.CompanyId,
                AppNamePattern = dialogResult.AppNamePattern,
                WindowTitlePattern = dialogResult.WindowTitlePattern,
                Category = dialogResult.Category,
                Priority = dialogResult.Priority,
                IsEnabled = dialogResult.IsEnabled
            });

            if (!result.IsSuccess)
            {
                MessageBox.Show(
                    string.IsNullOrWhiteSpace(result.Message) ? "Не удалось обновить правило." : result.Message,
                    "Ошибка редактирования",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            IsLoading = false;
            await LoadAsync(forceReload: true);
            MessageBox.Show(
                "Правило классификации обновлено.",
                "Правило обновлено",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ToggleRuleAsync(ActivityClassificationRule? rule)
    {
        if (rule is null)
        {
            return;
        }

        var enableRule = !rule.IsEnabled;
        var confirmation = MessageBox.Show(
            enableRule
                ? $"Включить правило для приложения \"{rule.AppNamePattern}\"?"
                : $"Выключить правило для приложения \"{rule.AppNamePattern}\"?",
            enableRule ? "Подтверждение включения" : "Подтверждение выключения",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirmation != MessageBoxResult.Yes)
        {
            return;
        }

        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            var result = await _rulesService.ToggleRuleAsync(rule.Id, enableRule);
            if (!result.IsSuccess)
            {
                MessageBox.Show(
                    string.IsNullOrWhiteSpace(result.Message) ? "Не удалось изменить статус правила." : result.Message,
                    "Ошибка изменения статуса",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            IsLoading = false;
            await LoadAsync(forceReload: true);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task DeleteRuleAsync(ActivityClassificationRule? rule)
    {
        if (rule is null)
        {
            return;
        }

        var confirmation = MessageBox.Show(
            $"Удалить правило для приложения \"{rule.AppNamePattern}\"?\n\nЭто действие нельзя отменить.",
            "Подтверждение удаления",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirmation != MessageBoxResult.Yes)
        {
            return;
        }

        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            var result = await _rulesService.DeleteRuleAsync(rule.Id);
            if (!result.IsSuccess)
            {
                MessageBox.Show(
                    string.IsNullOrWhiteSpace(result.Message) ? "Не удалось удалить правило." : result.Message,
                    "Ошибка удаления",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            IsLoading = false;
            await LoadAsync(forceReload: true);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private ActivityClassificationRuleEditorResult? ShowEditorDialog(ActivityClassificationRule? rule)
    {
        var window = new ActivityClassificationRuleEditorWindow(rule)
        {
            Owner = Application.Current.MainWindow
        };

        return window.ShowDialog() == true
            ? window.Result
            : null;
    }

    private void RaiseSummaryChanges()
    {
        OnPropertyChanged(nameof(HasRules));
        OnPropertyChanged(nameof(RulesCountText));
        OnPropertyChanged(nameof(EnabledRulesCount));
        OnPropertyChanged(nameof(DisabledRulesCount));
        OnPropertyChanged(nameof(IsEmptyStateVisible));
    }
}
