using System.Windows;
using Refline.Admin.Models;

namespace Refline.Admin.Views;

public partial class ActivityClassificationRuleEditorWindow : Window
{
    private readonly bool _isCreateMode;
    private bool _isSubmitting;

    public ActivityClassificationRuleEditorWindow(ActivityClassificationRule? rule)
    {
        InitializeComponent();

        _isCreateMode = rule is null;
        Result = null;

        CategoryComboBox.ItemsSource = new[]
        {
            new CategoryOption(ActivityCategory.Work, "Работа"),
            new CategoryOption(ActivityCategory.Communication, "Коммуникация"),
            new CategoryOption(ActivityCategory.ConditionalWork, "Условная работа"),
            new CategoryOption(ActivityCategory.Entertainment, "Развлечения"),
            new CategoryOption(ActivityCategory.System, "Системная"),
            new CategoryOption(ActivityCategory.Unknown, "Неизвестно")
        };

        if (_isCreateMode)
        {
            Title = "Добавить правило";
            DialogTitleText.Text = "Добавление правила";
            DialogSubtitleText.Text = "Создайте правило классификации для приложений и заголовков окон.";
            CategoryComboBox.SelectedValue = ActivityCategory.Work;
            PriorityTextBox.Text = "100";
            IsEnabledCheckBox.IsChecked = true;
            return;
        }

        Title = "Редактировать правило";
        DialogTitleText.Text = "Редактирование правила";
        DialogSubtitleText.Text = "Измените шаблоны, категорию, приоритет и состояние правила.";
        AppNamePatternTextBox.Text = rule!.AppNamePattern;
        WindowTitlePatternTextBox.Text = rule.WindowTitlePattern ?? string.Empty;
        CategoryComboBox.SelectedValue = rule.Category;
        PriorityTextBox.Text = rule.Priority.ToString();
        IsEnabledCheckBox.IsChecked = rule.IsEnabled;
    }

    public ActivityClassificationRuleEditorResult? Result { get; private set; }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isSubmitting)
        {
            return;
        }

        ValidationMessageText.Text = string.Empty;

        var appNamePattern = AppNamePatternTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(appNamePattern))
        {
            ValidationMessageText.Text = "Укажите AppNamePattern.";
            return;
        }

        if (CategoryComboBox.SelectedValue is not ActivityCategory category)
        {
            ValidationMessageText.Text = "Выберите категорию.";
            return;
        }

        if (!int.TryParse(PriorityTextBox.Text.Trim(), out var priority))
        {
            ValidationMessageText.Text = "Приоритет должен быть целым числом.";
            return;
        }

        if (priority is < 0 or > 1000)
        {
            ValidationMessageText.Text = "Приоритет должен быть в диапазоне от 0 до 1000.";
            return;
        }

        var windowTitlePattern = WindowTitlePatternTextBox.Text.Trim();

        Result = new ActivityClassificationRuleEditorResult
        {
            AppNamePattern = appNamePattern,
            WindowTitlePattern = string.IsNullOrWhiteSpace(windowTitlePattern) ? null : windowTitlePattern,
            Category = category,
            Priority = priority,
            IsEnabled = IsEnabledCheckBox.IsChecked == true
        };

        _isSubmitting = true;
        SaveButton.IsEnabled = false;
        CancelButton.IsEnabled = false;
        DialogResult = true;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private sealed record CategoryOption(ActivityCategory Value, string Text);
}
